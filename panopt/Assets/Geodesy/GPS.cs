using System;
using UnityEngine;

namespace Hagring
{
    // TODO move to Hagring.GNSS namspace, rename to GNSSLocalPosition ?
    public struct GPSPosition
    {
        public string Projection;
        public double North;
        public double East;
        public double Altitude;

        public GPSPosition(string Projection = null,
                           double North = 0, double East = 0,
                           double Altitude = 0)
        {
            this.Projection = Projection;
            this.North = North;
            this.East = East;
            this.Altitude = Altitude;
        }

        //
        // If specified position is in different projection, convert the
        // position to our projection.
        //
        GPSPosition ToSameProjection(GPSPosition pos)
        {
            if (string.Compare(Projection, pos.Projection, true) == 0)
            {
                /* already same projection, no conversation required */
                return pos;
            }

            /* convert position to WGS84 and then to our sweref projection */
            double longitude, latitude, elevation;
            GeodesyTransforms.toWGS84(pos, out longitude, out latitude, out elevation);

            return GeodesyTransforms.fromWGS84(Projection, longitude, latitude, elevation);
        }

        //
        // Calculate direction and distance between two GPS Positions,
        // expressed as a vector in meters.
        //
        // That is, the returned vector represent direction and distance
        // that you need to travel from this position to reach toPos position.
        //
        public Vector3 GetVector(GPSPosition toPos)
        {
            toPos = ToSameProjection(toPos);
            return new Vector3(
                (float)(toPos.East - East),
                (float)(toPos.Altitude - Altitude),
                (float)(toPos.North - North));
        }

        override public string ToString()
        {
            return String.Format("N {0} E {1} A {2} ({3})",
                North, East, Altitude, Projection);
        }

        ///
        /// Convert the HAPI style of SWEREF projection strings to the
        /// internally used format.
        ///
        /// E.g.
        ///
        ///   'tm' -> 'sweref_99_tm'
        ///   '13 30' -> 'sweref_99_13_30'
        ///
        /// TODO: rethink how this is handled generally, we should probably use
        /// enums internally to represent projections, so we don't have to parse
        /// the projection string all the time
        ///
        public static string hapiToInternalProj(string cloudProj)
        {
            /*
             * an hackish way to do the conversation
             */
            if (cloudProj.EndsWith("TM"))
            {
                return "sweref_99_tm";
            }

            return "sweref_99_" + cloudProj.Replace(' ', '_');
        }

        ///
        /// Convert internal style of SWEREF projections to the
        /// format used by HAPI.
        /// see comment above for more information
        ///
        public static string internalToHAPIProj(string internalProj)
        {
            if (internalProj.EndsWith("tm"))
            {
                return "TM";
            }

            return internalProj.Substring("sweref_99_".Length).Replace('_', ' ');
        }
    }
}
