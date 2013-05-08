using Ctr.FormatLibrary.Schema.Pwx.v1_0;
using Ctr.FormatLibrary.Schema.Tcx.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Ctr.FormatLibrary.Tcx
{
    /// <summary>
    /// Helpers for Tcx transformation.
    /// </summary>
    internal static class TcxHelpers
    {
        /// <summary>
        /// Transforms from Pwx sport type to Tcx sport type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Sport_t GetSportFromPwx(sportTypes type)
        { 
            switch(type)
            {
                case sportTypes.Bike:
                case sportTypes.MountainBike:
                    return Sport_t.Biking;
                case sportTypes.Run:
                    return Sport_t.Running;
                default:
                    return Sport_t.Other;
            }
        }

        /// <summary>
        /// Validate the tcx document
        /// </summary>
        /// <param name="xdoc"></param>
        public static void ValidateTcx(XDocument xdoc)
        {
            using (var schemaStream1 = System.Reflection.Assembly.GetAssembly(typeof(TcxHelpers)).GetManifestResourceStream(@"Ctr.FormatLibrary.Schema.Tcx.v2.ActivityExtensionv2.xsd"))
            using (var schemaStream2 = System.Reflection.Assembly.GetAssembly(typeof(TcxHelpers)).GetManifestResourceStream(@"Ctr.FormatLibrary.Schema.Tcx.v2.TrainingCenterDatabasev2.xsd"))
            {
                bool errorValidating = false;
                string errorsMsg = string.Empty;

                XmlSchema schema1 = XmlSchema.Read(schemaStream1, (sender, validationEvent) => { });
                XmlSchema schema2 = XmlSchema.Read(schemaStream2, (sender, validationEvent) => { });

                XmlSchemaSet set = new XmlSchemaSet();
                set.Add(schema1);
                set.Add(schema2);
                xdoc.Validate(set, (sender, validationEvent) =>
                {
                    errorValidating = true;
                    errorsMsg += validationEvent.Message;
                });

                if (errorValidating)
                {
                    throw new XmlSchemaException("Error validating output Tcx file: " + errorsMsg);
                }
            }
        }

        public class tcxLap
        {
            private List<pwxWorkoutSample> _points;
            public List<pwxWorkoutSample> points
            {
                get { return _points; }
                set
                {
                    _points = value;

                    // Fix if needed
                    ////if (points.Max(point => point.dist) == 0)
                    ////{
                    ////    double distanceAccumuled = 0;
                    ////    // If no distance was specified, pick it from the points spd * time
                    ////    for (int i = 0; i < points.Count - 1; i++)
                    ////    {
                    ////        distanceAccumuled += points[i].spd * (points[i + 1].timeoffset - points[i].timeoffset);
                    ////        points[i].dist += distanceAccumuled;
                    ////    }

                    ////    // difficult to calculate the last point distance... we approximate it to the last point distance
                    ////    points[points.Count - 1].dist = distanceAccumuled;
                    ////}

                    if (points.Max((p) => p.spd) == 0)
                    {
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            if (points[i].dist > 0 && points[i + 1].dist > 0)
                            {
                                double distanceBetweenPoints = points[i + 1].dist - points[i].dist;
                                double timeBetweenPoints = points[i + 1].timeoffset - points[i].timeoffset;

                                points[i].spd = distanceBetweenPoints / timeBetweenPoints;
                            }
                            points[points.Count - 1].spd = points[points.Count - 2].dist;
                        }
                    }
                }
            }

            public pwxWorkoutSegment segment;

            public double GetDistance()
            {
                double distance = 0;

                // Distance from the laps
                if (!segment.summarydata.distSpecified)
                {
                    distance = points.Max(point => point.dist);
                }
                else
                {
                    distance = segment.summarydata.dist;
                }

                return distance;
            }

            public double GetMaxSpeed()
            {
                double maxSpeed = 0;

                // For some reason the timex run trainer max speed is 96.6km/h
                double maxSpeedPoints = points.Max((p) => p.spd);

                for (int i = 0; i < points.Count - 1; i++)
                {
                    if (points[i].dist > 0 && points[i + 1].dist > 0)
                    {
                        double distanceBetweenPoints = points[i + 1].dist - points[i].dist;
                        double timeBetweenPoints = points[i + 1].timeoffset - points[i].timeoffset;

                        double avgSegmendSpeed = distanceBetweenPoints / timeBetweenPoints;
                        if (maxSpeed < avgSegmendSpeed)
                        {
                            maxSpeed = avgSegmendSpeed;
                        }
                    }
                }

                if (maxSpeedPoints > maxSpeed)
                {
                    maxSpeed = maxSpeedPoints;
                }

                // it can be also in the segment extensions:
                ////<extension>
                ////    <maxspd>19.37</maxspd>
                ////    <calories>80</calories>
                ////</extension>

                return maxSpeed;
            }

            public ushort GetCalories()
            {
                ushort calories = (ushort)(segment.summarydata.work * 0.239);

                if (calories == 0)
                {
                    // sometimes it comes in the extensions:
                    ////<segment>
                    ////  <name>Lap 1</name>
                    ////  <summarydata>
                    ////    <beginning>2</beginning>
                    ////    <duration>351</duration>
                    ////    <hr max="142" min="0" avg="123" />
                    ////  </summarydata>
                    ////  <extension>
                    ////    <maxspd>19.37</maxspd>
                    ////    <calories>80</calories>
                    ////  </extension>
                    ////</segment>
                }

                return calories;
            }
        
        }

        public static IEnumerable<tcxLap> GetTcxLaps(List<pwxWorkoutSample> pointList, pwxWorkoutSegment[] segments)
        {
            for (int i = 0; i < segments.Count() - 1; i++)
            {
                tcxLap lap = new tcxLap();
                lap.points = GetSamplePointsByTime(pointList, segments[i].summarydata.beginning, segments[i + 1].summarydata.beginning).ToList();
                lap.segment = segments[i];

                yield return lap;
            }

            tcxLap lastlap = new tcxLap();
            lastlap.points = GetSamplePointsByTime(pointList, segments.Last().summarydata.beginning, double.MaxValue).ToList();
            lastlap.segment = segments.Last();

            yield return lastlap;
        }

        public static IEnumerable<pwxWorkoutSample> GetSamplePointsByTime(List<pwxWorkoutSample> totalList, double startTime, double endtime)
        {
            return totalList.Select((point) => point).Where((point) => point.timeoffset >= startTime && point.timeoffset < endtime);
        }
    }
}
