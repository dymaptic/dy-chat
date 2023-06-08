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
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace SDKExamples.GeodatabaseSDK
{
  /// <summary>
  /// Illustrates how to search from a FeatureClass.
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
  public class FeatureClassSearch
  {
    /// <summary>
    /// In order to illustrate that Geodatabase calls have to be made on the MCT
    /// </summary>
    /// <returns></returns>
    public async Task FeatureClassSearchAsync()
    {
      await QueuedTask.Run(() => MainMethodCode());
    }

    public void MainMethodCode()
    {
      using (Geodatabase fileGeodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(@"C:\Data\LocalGovernment.gdb"))))
      using (FeatureClass featureClass   = fileGeodatabase.OpenDataset<FeatureClass>("PollingPlace"))
      {
        FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition();

        int areaFieldIndex = featureClassDefinition.FindField(featureClassDefinition.GetAreaField());

        // ******************** WITHOUT USING RECYCLING ********************

        QueryFilter queryFilter = new QueryFilter { WhereClause = "CITY = 'Plainfield'" };

        List<Feature> features = new List<Feature>();

        // Searching is similar to that of Table when using Queryfilter.
        using (RowCursor cursor = featureClass.Search(queryFilter, false))
        {
          while (cursor.MoveNext())
          {
            // Each object returned by RowCursor.Current can be cast to a Feature object if the Search was performed on a FeatureClass.
            features.Add(cursor.Current as Feature);
          }
        }
        
        IEnumerable<Feature> featuresHavingShapePopulated = features.Where(feature => !feature.GetShape().IsEmpty);

        // Since Feature encapsulates unmanaged resources, it is important to remember to call Dispose() on every entry in the list when
        // the list is no longer in use.  Alternatively, do not add the features to the list.  Instead, process each of them inside the cursor.

        Dispose(features);

        // ******************** USING RECYCLING ********************

        using (RowCursor recyclingCursor = featureClass.Search(queryFilter))
        {
          while (recyclingCursor.MoveNext())
          {
            // Similar to a RowCursor on a Table, when a MoveNext is executed, the same feature object is populated with the next feature's details
            // So any processing should be done on the Current Feature before the MoveNext is called again.

            Feature feature = (Feature)recyclingCursor.Current;

            if (Convert.ToDouble(feature[areaFieldIndex]) > 500)
              Console.WriteLine(feature.GetShape().ToXml());
          }
        } 
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
      
      using (Geodatabase geodatabase                 = new Geodatabase(connectionProperties))
      using (FeatureClass schoolBoundaryFeatureClass = geodatabase.OpenDataset<FeatureClass>("LocalGovernment.GDB.SchoolBoundary"))
      {
        // Using a spatial query filter to find all features which have a certain district name and lying within a given Polygon.
        SpatialQueryFilter spatialQueryFilter = new SpatialQueryFilter
        {
          WhereClause    = "DISTRCTNAME = 'Indian Prairie School District 204'",
          FilterGeometry = new PolygonBuilderEx(new List<Coordinate2D>
          {
            new Coordinate2D(1021880, 1867396),
            new Coordinate2D(1028223, 1870705),
            new Coordinate2D(1031165, 1866844),
            new Coordinate2D(1025373, 1860501),
            new Coordinate2D(1021788, 1863810)
          }).ToGeometry(),

          SpatialRelationship = SpatialRelationship.Within
        };
        
        using (RowCursor indianPrairieCursor = schoolBoundaryFeatureClass.Search(spatialQueryFilter, false))
        {
          while (indianPrairieCursor.MoveNext())
          {
            using (Feature feature = (Feature)indianPrairieCursor.Current)
            {
              // Process the feature.
              Console.WriteLine(feature.GetObjectID());
            }
          }
        }
      }
    }

    private static void Dispose<T>(IEnumerable<T> iterator) where T : CoreObjectsBase
    {
      if (iterator != null)
      {
        foreach (T coreObject in iterator)
        {
          if (coreObject != null)
            coreObject.Dispose();
        }
      }
    }
  }
}