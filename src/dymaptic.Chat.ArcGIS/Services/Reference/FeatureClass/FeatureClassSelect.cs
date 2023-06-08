/*

   Copyright 2018 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace SDKExamples.GeodatabaseSDK
{
  /// <summary>
  /// Illustrates how to select from a FeatureClass.
  /// </summary>
  /// 
  /// <remarks>
  /// <para>
  /// While it is true classes that are derived from the <see cref="ArcGIS.Core.CoreObjectsBase"/> super class 
  /// consumes native resources (e.g., <see cref="ArcGIS.Core.Data.Geodatabase"/> or <see cref="ArcGIS.Core.Data.FeatureClass"/>), 
  /// you can rest assured that the garbage collector will properly dispose of the unmanaged resources during 
  /// finalization.  However, there are certain workflows that require a <b>deterministic</b> finalization of the 
  /// <see cref="ArcGIS.Core.Data.Geodatabase"/>.  Consider the case of a file geodatabase that needs to be deleted 
  /// on the fly at a particular moment.  Because of the <b>indeterministic</b> nature of garbage collection, we can't
  /// count on the garbage collector to dispose of the Geodatabase object, thereby removing the <b>lock(s)</b> at the  
  /// moment we want. To ensure a deterministic finalization of important native resources such as a 
  /// <see cref="ArcGIS.Core.Data.Geodatabase"/> or <see cref="ArcGIS.Core.Data.FeatureClass"/>, you should declare 
  /// and instantiate said objects in a <b>using</b> statement.  Alternatively, you can achieve the same result by 
  /// putting the object inside a try block and then calling Dispose() in a finally block.
  /// </para>
  /// <para>
  /// In general, you should always call Dispose() on the following types of objects: 
  /// </para>
  /// <para>
  /// - Those that are derived from <see cref="ArcGIS.Core.Data.Datastore"/> (e.g., <see cref="ArcGIS.Core.Data.Geodatabase"/>).
  /// </para>
  /// <para>
  /// - Those that are derived from <see cref="ArcGIS.Core.Data.Dataset"/> (e.g., <see cref="ArcGIS.Core.Data.Table"/>).
  /// </para>
  /// <para>
  /// - <see cref="ArcGIS.Core.Data.RowCursor"/> and <see cref="ArcGIS.Core.Data.RowBuffer"/>.
  /// </para>
  /// <para>
  /// - <see cref="ArcGIS.Core.Data.Row"/> and <see cref="ArcGIS.Core.Data.Feature"/>.
  /// </para>
  /// <para>
  /// - <see cref="ArcGIS.Core.Data.Selection"/>.
  /// </para>
  /// <para>
  /// - <see cref="ArcGIS.Core.Data.VersionManager"/> and <see cref="ArcGIS.Core.Data.Version"/>.
  /// </para>
  /// </remarks>
  public class FeatureClassSelect
  {
    /// <summary>
    /// In order to illustrate that Geodatabase calls have to be made on the MCT
    /// </summary>
    /// <returns></returns>
    public async Task FeatureClassSelectAsync()
    {
      await QueuedTask.Run(() => MainMethodCode());
    }

    public void MainMethodCode()
    {
      using (Geodatabase fileGeodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(@"C:\Data\LocalGovernment.gdb"))))
      using (FeatureClass featureClass   = fileGeodatabase.OpenDataset<FeatureClass>("PollingPlace"))
      {
        QueryFilter queryFilter = new QueryFilter { WhereClause = "CITY = 'Plainfield'" };

        // For Selecting all matching entries.
        Selection selection = featureClass.Select(queryFilter, SelectionType.ObjectID, SelectionOption.Normal);

        // Number of records that match the QueryFilter criteria.
        long numberOfMatchingRecords = selection.GetCount();

        // The objectIDs which match the QueryFilter criteria.
        IReadOnlyList<long> readOnlyList = selection.GetObjectIDs();
      }
      
       // Opening a Non-Versioned SQL Server instance.

      DatabaseConnectionProperties connectionProperties = new DatabaseConnectionProperties(EnterpriseDatabaseType.SQLServer)
      {
        AuthenticationMode = AuthenticationMode.DBMS,

        // Where testMachine is the machine where the instance is running and testInstance is the name of the SqlServer instance.
        Instance = @"testMachine\testInstance",

        // Provided that a database called LocalGovernment has been created on the testInstance and geodatabase has been enabled on the database.
        Database = "LocalGovernment",

        // Provided that a login called gdb has been created and corresponding schema has been created with the required permissions.
        User     = "gdb",
        Password = "password",
        Version  = "dbo.DEFAULT"
      };

      using (Geodatabase geodatabase             = new Geodatabase(connectionProperties))
      using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>("LocalGovernment.GDB.FacilitySite"))
      {
        List<Coordinate2D> newCoordinates = new List<Coordinate2D>
        {
          new Coordinate2D(1021570, 1880583),
          new Coordinate2D(1028730, 1880994),
          new Coordinate2D(1029718, 1875644),
          new Coordinate2D(1021405, 1875397)
        };

        SpatialQueryFilter spatialFilter = new SpatialQueryFilter 
        {
          WhereClause         = "FCODE = 'Park'",
          FilterGeometry      = new PolygonBuilderEx(newCoordinates).ToGeometry(),
          SpatialRelationship = SpatialRelationship.Crosses
        };

        // For Selecting all matching entries.
        Selection anotherSelection = enterpriseFeatureClass.Select(spatialFilter, SelectionType.ObjectID, SelectionOption.Normal);

        // This can be used to get one record which matches the criteria. No assumptions can be made about which record satisfying the 
        // criteria is selected.
        Selection onlyOneSelection = enterpriseFeatureClass.Select(spatialFilter, SelectionType.ObjectID, SelectionOption.OnlyOne);

        // This will always be one.
        long singleRecordCount = onlyOneSelection.GetCount();

        // This will always have one record.
        IReadOnlyList<long> listWithOneValue = onlyOneSelection.GetObjectIDs();

        // This can be used to obtain a empty selction which can be used as a container to combine results from different selections.
        Selection emptySelection = enterpriseFeatureClass.Select(spatialFilter, SelectionType.ObjectID, SelectionOption.Empty);

        // This will always be zero.
        long zeroCount = emptySelection.GetCount();

        // This will always have zero record.
        IReadOnlyList<long> emptyList = emptySelection.GetObjectIDs();

        // If you want to select all the records in a table.
        Selection allRecordSelection = enterpriseFeatureClass.Select(null, SelectionType.ObjectID, SelectionOption.Normal);

        // This will give the count of records in a table.
        long numberOfRecordsInATable = allRecordSelection.GetCount();
      }
    }
  }
}