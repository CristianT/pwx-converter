using Ctr.FormatLibrary.Gpx;
using Ctr.FormatLibrary.Schema.Pwx.v1_0;
using Ctr.FormatLibrary.Schema.Tcx.v2;
using Ctr.FormatLibrary.Tcx;
using Ctr.FormatLibrary.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Ctr.FormatLibrary.Pwx
{
    /// <summary>
    /// Converter for Pwx files.
    /// </summary>
    public class PwxConverter
    {
        /// <summary>
        /// Initializes the PwxConverter with a file path.
        /// </summary>
        /// <param name="file">Path to the file.</param>
        public PwxConverter(string file)
            : this(XDocument.Load(file))
        {
        }

        /// <summary>
        /// Initializes the PwxConverter with a stream to an Xml.
        /// </summary>
        /// <param name="file">The </param>
        public PwxConverter(Stream file)
            : this(XDocument.Load(file))
        {
        }

        /// <summary>
        /// Initializes the converter with xml document of the file.
        /// </summary>
        /// <param name="file"></param>
        public PwxConverter(XDocument file)
        {
            this.originalFile = Serializer.Deserialize<pwx>(file.ToString()) as pwx;
        }

        private pwx originalFile;

        /// <summary>
        /// Converts the main document to a Gpx file.
        /// </summary>
        /// <returns></returns>
        public gpxType ConvertToGpx()
        {
            // Base document
            gpxType gpx = new gpxType();
            gpx.version = "1.1";
            gpx.creator = "PwxConverter";

            // The metadata
            gpx.metadata = new metadataType();
            gpx.metadata.time = DateTime.Now;

            List<trkType> tracks = new List<trkType>();
            foreach (var workout in originalFile.workout)
            {
                // A track for this workout
                var track = new trkType();
                track.type = Enum.GetName(typeof(sportTypes), workout.sportType);
                tracks.Add(track);

                // The laps, only one supported
                List<trksegType> laps = new List<trksegType>();
                var lap = new trksegType();
                laps.Add(lap);

                // Pwx start time
                DateTime pwxStartDate = workout.time;
                
                // The points
                List<wptType> points = new List<wptType>();
                foreach (var point in workout.sample)
                {
                    var gpxPoint = new wptType();
                    gpxPoint.ele = (decimal)point.alt;
                    gpxPoint.eleSpecified = true;
                    gpxPoint.lat = (decimal)point.lat;
                    gpxPoint.lon = (decimal)point.lon;
                    gpxPoint.timeSpecified = true;
                    gpxPoint.time = pwxStartDate + new TimeSpan(0, 0, 0, (int)point.timeoffset);
                    
                    points.Add(gpxPoint);
                }

                lap.trkpt = points.ToArray();
                track.trkseg = laps.ToArray();
            }
            
            gpx.trk = tracks.ToArray();

            return gpx;
        }

        /// <summary>
        /// Saves the document as Gpx in the specified path.
        /// </summary>
        /// <param name="file"></param>
        public void SaveAsGpx(string file)
        {
            var gpx = this.ConvertToGpx();
            var xdoc = Serializer.Serialize(gpx);

            GpxHelpers.ValidateGpx(xdoc);

            xdoc.Save(file);
        }

        /// <summary>
        /// Converts the file to a Tcx file.
        /// </summary>
        /// <returns></returns>
        public TrainingCenterDatabase_t ConverToTcx()
        {
            // The main file
            TrainingCenterDatabase_t tcx = new TrainingCenterDatabase_t();
            tcx.Activities = new ActivityList_t();

            // The workouts
            List<Activity_t> activities = new List<Activity_t>();
            foreach (var workout in originalFile.workout)
            {
                // First of all remove the points with lat=0, long=0, dist=0 and alt=0
                var samplePoints = workout.sample.ToList();
                samplePoints.RemoveAll((sample) => sample.lat == 0 && sample.lon == 0 && sample.dist == 0 && sample.alt == 0);

                TimeZone zone = TimeZone.CurrentTimeZone;
                DateTime pwxStartDate = zone.ToUniversalTime(workout.time);

                Activity_t activity = new Activity_t();
                activities.Add(activity);

                activity.Sport = TcxHelpers.GetSportFromPwx(workout.sportType);
                activity.Id = pwxStartDate;

                List<ActivityLap_t> workoutLaps = new List<ActivityLap_t>();
                List<TcxHelpers.tcxLap> pwxLaps;
                if (workout.segment != null)
                {
                    pwxLaps = TcxHelpers.GetTcxLaps(samplePoints, workout.segment).ToList();
                }
                else
                {
                    pwxWorkoutSegment s = new pwxWorkoutSegment();
                    s.summarydata = workout.summarydata;
                    pwxLaps = TcxHelpers.GetTcxLaps(samplePoints, new pwxWorkoutSegment[] { s }).ToList();
                }

                foreach (var pwxLapInfo in pwxLaps)
                {
                    var summaryData = pwxLapInfo.segment.summarydata;

                    ActivityLap_t lap = new ActivityLap_t();
                    lap.StartTime = pwxStartDate + new TimeSpan(0, 0, (int)summaryData.beginning);
                    lap.TotalTimeSeconds = summaryData.duration;
                    lap.DistanceMeters = pwxLapInfo.GetDistance();
                    lap.MaximumSpeed = pwxLapInfo.GetMaxSpeed();
                    lap.MaximumSpeedSpecified = true;
                    
                    // convert the work in KJ to calories
                    lap.Calories = pwxLapInfo.GetCalories();

                    //////////////////////////////////////////////////////////////////////////////////////////////
                    // Average speed
                    ActivityLapExtension_t extension = new Ctr.FormatLibrary.Schema.Tcx.v2.ActivityLapExtension_t();
                    extension.AvgSpeed = lap.DistanceMeters / lap.TotalTimeSeconds;
                    extension.AvgSpeedSpecified = true;
                    var extensionXml = new XmlDocument();
                    extensionXml.LoadXml(Serializer.Serialize(extension).ToString());

                    lap.Extensions = new Extensions_t()
                    {
                        Any = new System.Xml.XmlElement[] { extensionXml.DocumentElement }
                    };
                    //////////////////////////////////////////////////////////////////////////////////////////////

                    //Hr
                    if (summaryData.hr != null)
                    {
                        if (summaryData.hr.avgSpecified)
                        {
                            lap.AverageHeartRateBpm = new HeartRateInBeatsPerMinute_t();
                            lap.AverageHeartRateBpm.Value = (byte)summaryData.hr.avg;
                        }

                        if (summaryData.hr.maxSpecified)
                        {
                            lap.MaximumHeartRateBpm = new HeartRateInBeatsPerMinute_t();
                            lap.MaximumHeartRateBpm.Value = (byte)summaryData.hr.max;
                        }
                    }

                    // The points
                    List<Trackpoint_t> points = new List<Trackpoint_t>();
                    foreach (var point in pwxLapInfo.points)
                    {
                        Trackpoint_t tcxPoint = new Trackpoint_t();

                        tcxPoint.Time = pwxStartDate + new TimeSpan(0, 0, 0, (int)point.timeoffset);
                        tcxPoint.Position = new Position_t()
                        {
                            LatitudeDegrees = point.lat,
                            LongitudeDegrees = point.lon
                        };
                        tcxPoint.AltitudeMeters = point.alt;
                        tcxPoint.AltitudeMetersSpecified = point.altSpecified;
                        
                        tcxPoint.DistanceMeters = point.dist;
                        tcxPoint.DistanceMetersSpecified = point.distSpecified;

                        if (point.hrSpecified)
                        {
                            tcxPoint.HeartRateBpm = new HeartRateInBeatsPerMinute_t() { Value = point.hr };
                        }

                        points.Add(tcxPoint);
                    }

                    lap.Track = points.ToArray();
                    workoutLaps.Add(lap);
                }

                activity.Lap = workoutLaps.ToArray();

                // The activity metadata
                var device = new Device_t();
                activity.Creator = device;

                device.Name = workout.device.make + " " + workout.device.model;
                device.Version = new Version_t() { VersionMajor = 0, VersionMinor = 0 };
                device.UnitId = 0;
            }
            tcx.Activities.Activity = activities.ToArray();

            // The footer
            var application = new Application_t();
            tcx.Author = application;
            
            application.Name = "PwxConverter";
            application.Build = new Build_t()
            {
                Builder = "",
                Time = "2013-01-25",
                Type = BuildType_t.Alpha,
                TypeSpecified = true,
                Version = new Version_t() { VersionMajor = 0, VersionMinor = 0 }
            };
            application.LangID = "EN";
            application.PartNumber = "000-A0000-00";
            
            return tcx;
        }
        

        /// <summary>
        /// Saves the document as Gpx in the specified path.
        /// </summary>
        /// <param name="file"></param>
        public void SaveAsTcx(string file)
        {
            var tcx = this.ConverToTcx();
            var xdoc = Serializer.Serialize(tcx);
            
            TcxHelpers.ValidateTcx(xdoc);

            xdoc.Save(file);
        }

        /// <summary>
        /// Saves the document as Fit file in the specified path.
        /// </summary>
        /// <param name="file"></param>
        public void SaveAsFit(string file)
        {
            /*
            Dynastream.Fit.FileIdMesg fileIdMesg = new Dynastream.Fit.FileIdMesg();

            Dynastream.Fit.UserProfileMesg myUserProfile = new Dynastream.Fit.UserProfileMesg();

            List<Dynastream.Fit.RecordMesg> messages = new List<Dynastream.Fit.RecordMesg>();

            //Dynastream.Fit.ActivityMesg

            // The workouts
            List<Activity_t> activities = new List<Activity_t>();
            foreach (var workout in originalFile.workout)
            {
                DateTime pwxStartDate = workout.time;

                Activity_t activity = new Activity_t();
                activities.Add(activity);

                activity.Sport = TcxHelpers.GetSportFromPwx(workout.sportType);
                activity.Id = pwxStartDate;

                ActivityLap_t lap = new ActivityLap_t();
                lap.StartTime = pwxStartDate;
                lap.TotalTimeSeconds = workout.sample.Max(point => point.timeoffset);
                lap.DistanceMeters = workout.sample.Max(point => point.dist) / 1000;
                lap.MaximumSpeed = workout.sample.Max(point => point.spd);
                // lap.Calories -> no calories info in my sample pwx

                // The points
                List<Trackpoint_t> points = new List<Trackpoint_t>();
                foreach (var point in workout.sample)
                {
                    Trackpoint_t tcxPoint = new Trackpoint_t();

                    tcxPoint.Time = pwxStartDate + new TimeSpan(0, 0, 0, (int)point.timeoffset);
                    tcxPoint.Position = new Position_t()
                    {
                        LatitudeDegrees = point.lat,
                        LongitudeDegrees = point.lon
                    };
                    tcxPoint.AltitudeMeters = point.alt;
                    tcxPoint.DistanceMeters = point.dist;

                    points.Add(tcxPoint);
                }

                lap.Track = points.ToArray();
                activity.Lap = new ActivityLap_t[] { lap };
            }
            tcx.Activities.Activity = activities.ToArray();















            // Save
            using (FileStream fitDest = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                // Create file encode object
                Dynastream.Fit.Encode encodeDemo = new Dynastream.Fit.Encode();
                // Write header
                encodeDemo.Open(fitDest);

                // Encode each message, a definition message is automatically generated and output if necessary
                encodeDemo.Write(fileIdMesg);
                encodeDemo.Write(myUserProfile);

                // Update header datasize and file CRC
                encodeDemo.Close();
            }
            */
        }
    }
}
