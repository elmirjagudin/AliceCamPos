namespace Hagring.GNSS
{
    public enum FixQuality
    {
        Invalid = 0,    /* Fix not valid  */
        GNSS = 1,       /* GNSS fix */
        DGPS = 2,       /* Differential GNSS fix */
        PPS = 3,        /* PPS fix */
        RTK = 4,        /* RTK fix */
        FloatRTK = 5,   /* float RTK */
        Estimated = 6,  /* estimated, dead reckoning */
        Manual = 7,     /* manual input mode */
        Simulation = 8, /* simulation mode */
    }

    public struct GGAMessage
    {
        public double Timestamp;

        public double Longitude;
        public double Latitude;
        public double Elevation;

        public FixQuality Quality;

        public override string ToString()
        {
            /* for debugging purposes */
            return string.Format("ts {0} long {1} lat {2} q {3}",
                                 Timestamp, Longitude, Latitude, Quality);
        }
    }
}
