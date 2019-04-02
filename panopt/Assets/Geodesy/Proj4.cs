using System;
using System.Runtime.InteropServices;

///
/// simple wrapper for the parts of Proj4 C API we are using
///
public class Proj4
{
    public struct PJ_COORD
    {
        public double east, north, up, _unused;
    }

    public enum PJ_DIRECTION : int
    {
        PJ_FWD = 1,      /* Forward    */
        PJ_IDENT = 0,    /* Do nothing */
        PJ_INV = -1      /* Inverse    */
    };

    [DllImport("proj")]
    static extern void proj_context_set_search_paths(
        IntPtr ctx, int count, string[] paths);

    [DllImport("proj")]
    static extern IntPtr proj_create(IntPtr ctx, string definition);


    [DllImport("proj")]
    public static extern PJ_COORD proj_coord(double x, double y, double z = 0, double t = 0);

    [DllImport("proj")]
    public static extern PJ_COORD proj_trans(IntPtr Proj, PJ_DIRECTION direction, PJ_COORD coord);

    ///
    /// wrapper around proj_create() that throws an
    /// exception if we fail to create projection object
    ///
    public static IntPtr CreateProj(string definition)
    {
        var Proj = proj_create(IntPtr.Zero, definition);
        if (Proj == IntPtr.Zero)
        {
            throw new Exception(
                string.Format("failed to create projection, using definition '{0}'", definition));
        }

        return Proj;
    }

    ///
    /// wrapper around proj_context_set_search_paths() that sets default
    /// context's search path to exatly one specified directory
    ///
    public static void SetSearchPath(string Path)
    {
        proj_context_set_search_paths(IntPtr.Zero, 1, new[] { Path });
    }
}
