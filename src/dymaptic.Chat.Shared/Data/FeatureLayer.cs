using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Chat.Shared.Data
{
    public class FeatureLayer
    {
        public FeatureLayer()
        {

        }

        public FeatureLayer(string featureLayerName, string body, bool isMine)
        {
            FeatureLayerName = featureLayerName;
            Body = body;
            IsMine = isMine;
        }

        /// <summary>
        /// General Properties, Not included Visibility Range and Refresh Rate
        /// </summary>
        public string FeatureLayerName { get; set; }

        /// <summary>
        /// Metadata Properties
        /// </summary>
        public string Tags { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Credits { get; set; }
        public string UseLimitations { get; set; }
        /// <summary>
        /// Source Properties
        /// </summary>
        public string DataType { get; set; }
        // References a json file?
        public string Server { get; set; }
        public string VerticalUnits { get; set; }

        


    }

    public class Extent 
    {
        public Extent() { }

        public Extent(double top, double bottom, double right, double left)
        {
            XMin = left;
            YMin = bottom;
            XMax = right;
            YMax = top;
        }
        /// <summary>
        /// Extent Properties (Units are designated above in data source)
        /// </summary>
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
    }

    public class SpatialReference 
    {
        public SpatialReference() { }

        public SpatialReference(string projectedCoordinateSystem, string projection, int wkid, int previousWkid, string authority, string linearUnit, double falseEasting, double falseNorthing, double centralMeridian, double standardParallel, double auxiliarySphereType, string geographicCoordinateSystem, int wkidGCS, string authorityGCS, string angularUnitGCS, string primeMeridianGCS, string datum, string spheroid, double semimajorAxis, double semiminorAxis, double inverseFlattening )
        {

            Projection = projection;
            ProjectedCoordinateSystem = projectedCoordinateSystem;
            Wkid = wkid;
            PreviousWkid = previousWkid;
            Authority = authority;
            LinearUnit = linearUnit;
            FalseEasting = falseEasting;
            FalseNorthing = falseNorthing;
            CentralMeridian = centralMeridian;
            StandardParallel = standardParallel;
            AuxiliarySphereType = auxiliarySphereType;
            GeographicCoordinateSystem = geographicCoordinateSystem;    // aka GCS
            WkidGCS = wkidGCS;                      // WkidGCS is the same as Wkid for GCS
            AuthorityGCS = authorityGCS;            // AuthorityGCS is the same as Authority for GCS
            AngularUnitGCS = angularUnitGCS;        // AngularUnitGCS is the same as AngularUnit for GCS
            PrimeMeridianGCS = primeMeridianGCS;    // PrimeMeridianGCS is the same as PrimeMeridian for GCS
            Datum = datum;
            Spheroid = spheroid;
            SemimajorAxis = semimajorAxis;
            SemiminorAxis = semiminorAxis;
            InverseFlattening = inverseFlattening;
        }
        /// <summary>
        /// Projected Coordinate System Properties-Spaital Reference
        /// </summary>
        public string ProjectedCoordinateSystem { get; set; }
        public string Projection { get; set; }  
        public int Wkid { get; set; }
        public int PreviousWkid { get; set; }
        public string Authority { get; set; }
        public string LinearUnit { get; set; }
        public double FalseEasting { get; set; }
        public double FalseNorthing { get; set; }
        public double CentralMeridian { get; set; }
        public double StandardParallel { get; set; }
        public double AuxiliarySphereType { get; set; }
        /// <summary>
        /// Grapic Coordinate System Properties-Spatial Reference (See notes above about GCS references)
        /// </summary>
        public string GeographicCoordinateSystem { get; set; }
        public int WkidGCS { get; set; }
        public string AuthorityGCS { get; set; }
        public string AngularUnitGCS { get; set; }
        public string PrimeMeridianGCS { get; set; }
        public string Datum { get; set; }
        public string Spheroid { get; set; }
        public double SemimajorAxis { get; set; }
        public double SemiminorAxis { get; set; }
        public double InverseFlattening { get; set; }

    }

    public class Domain
}
