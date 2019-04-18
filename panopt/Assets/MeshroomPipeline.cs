using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable 0649
abstract class Node
{
    public string nodeType;
    public object parallelization;
    public Dictionary<string, string> uids = new Dictionary<string, string>() { {"0",  "0" }};
    public string internalFolder = "{cache}/{nodeType}/{uid0}/";
    public object inputs;
    public object outputs;

    protected Node(string nodeType, int blocksize, int size, int split)
    {
        this.nodeType = nodeType;
        parallelization = new
        {
            blocksize = blocksize,
            size = size,
            split = split,
        };
    }
}

class CameraInit : Node
{
    const int INTRINSIC_ID = 1;
    const int MAGIC_LARGE_NUMBER = 60903726;

    List<object> GetViewpoints(IEnumerable<string> images)
    {
        var views = new List<object>();

        uint i = 0;
        foreach (var img in images)
        {
            views.Add(new
            {
                /*
                 * seems that view and pose ID must be 'large numbers',
                 * otherwise the meshroom_compute will fail on
                 * 'DepthMap' node
                 */
                viewId = MAGIC_LARGE_NUMBER + i,
                poseId = MAGIC_LARGE_NUMBER + i,
                path = img,
                intrinsicId = INTRINSIC_ID,
                rigId = -1,
                subPoseId = -1,
                metadata = "",
            });

            i += 1;
        }

        return views;
    }

    public CameraInit(double FocalLength, int Width, int Height, IEnumerable<string> Images,
                      string SensorDatabase)
        : base("CameraInit", 0, 50, 1)
    {
        inputs = new
        {
            viewpoints = GetViewpoints(Images),
            intrinsics = new object[]
            {
                new
                {
                    intrinsicId =  INTRINSIC_ID,
                    pxInitialFocalLength = FocalLength,
                    pxFocalLength = FocalLength,
                    type = "radial3",
                    width = Width,
                    height = Height,
                    serialNumber = "unknown",
                    principalPoint = new { x = Width / 2, y = Height / 2 },
                    initializationMode = "unknown",
                    distortionParams = new object[] { 0.0, 0.0, 0.0 },
                    locked = false
                },
            },
            sensorDatabase = SensorDatabase,
            defaultFieldOfView = 45.0,
            groupCameraFallback = "folder",
            verboseLevel = "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/cameraInit.sfm"
        };
    }
}

class ImageMatching : Node
{
    public ImageMatching(string VocTree) : base("ImageMatching", 0, 50, 1)
    {
        inputs = new
        {
            input = "{FeatureExtraction_1.input}",
            featuresFolders = new object[] { "{FeatureExtraction_1.output}" },
            tree = VocTree,
            weights = "",
            minNbImages = 200,
            maxDescriptors = 500,
            nbMatches = 50,
            verboseLevel = "info",
        };

        outputs = new { output = "{cache}/{nodeType}/{uid0}/imageMatches.txt" };
    }
}

class FeatureMatching : Node
{
    public FeatureMatching() : base("FeatureMatching", 20, 50, 3)
    {
        inputs = new
        {
            input = "{ImageMatching_1.input}",
            featuresFolders = "{ImageMatching_1.featuresFolders}",
            imagePairsList = "{ImageMatching_1.output}",
            describerTypes = new object[] {"sift"},
            photometricMatchingMethod = "ANN_L2",
            geometricEstimator = "acransac",
            geometricFilterType = "fundamental_matrix",
            distanceRatio = 0.8,
            maxIteration = 2048,
            geometricError = 0.0,
            maxMatches = 0,
            savePutativeMatches = false,
            guidedMatching = false,
            exportDebugFiles = false,
            verboseLevel = "info",
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/imageMatches.txt"
        };
    }
}

class FeatureExtraction : Node
{
    public FeatureExtraction(): base("FeatureExtraction", 40, 50, 2)
    {
        inputs = new
        {
            input = "{CameraInit_1.output}",
            describerTypes = new object[] {
                "sift"
            },
            forceCpuExtraction = true,
            describerPreset = "normal",
            verboseLevel = "info",
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/"
        };
    }
}


class StructureFromMotion : Node
{
    public StructureFromMotion() : base("StructureFromMotion", 0, 50, 1)
    {
        inputs = new
        {
            input = "{FeatureMatching_1.input}",
            featuresFolders = "{FeatureMatching_1.featuresFolders}",
            matchesFolders = new object[] { "{FeatureMatching_1.output}" },
            describerTypes = new object[] { "sift" },
            localizerEstimator = "acransac",
            localizerEstimatorMaxIterations = 4096,
            localizerEstimatorError = 0.0,
            lockScenePreviouslyReconstructed = false,
            useLocalBA = true,
            localBAGraphDistance = 1,
            maxNumberOfMatches = 0,
            minInputTrackLength = 2,
            minNumberOfObservationsForTriangulation = 2,
            minAngleForTriangulation = 3.0,
            minAngleForLandmark = 2.0,
            maxReprojectionError = 4.0,
            minAngleInitialPair = 5.0,
            maxAngleInitialPair = 40.0,
            useOnlyMatchesFromInputFolder = false,
            useRigConstraint = true,
            lockAllIntrinsics = false,
            initialPairA = "",
            initialPairB = "",
            interFileExtension = ".abc",
            verboseLevel = "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/sfm.abc",
            outputViewsAndPoses = "{cache}/{nodeType}/{uid0}/cameras.sfm",
            extraInfoFolder = "{cache}/{nodeType}/{uid0}/"
        };
    }
}

class PrepareDenseScene : Node
{
    public PrepareDenseScene() : base("PrepareDenseScene", 40, 50, 2)
    {
        inputs = new
        {
            input =  "{StructureFromMotion_1.output}",
            imagesFolders = new object[] {},
            outputFileType = "exr",
            saveMetadata = true,
            saveMatricesTxtFiles = false,
            verboseLevel = "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/"
        };
    }
}

class DepthMap : Node
{
    public DepthMap() : base ("DepthMap", 3, 50, 17)
    {
        inputs = new
        {
            input = "{PrepareDenseScene_1.input}",
            imagesFolder = "{PrepareDenseScene_1.output}",
            downscale = 2,
            minViewAngle = 2.0,
            maxViewAngle = 70.0,
            sgmMaxTCams = 10,
            sgmWSH = 4,
            sgmGammaC = 5.5,
            sgmGammaP = 8.0,
            refineMaxTCams = 6,
            refineNSamplesHalf = 150,
            refineNDepthsToRefine = 31,
            refineNiters = 100,
            refineWSH = 3,
            refineSigma = 15,
            refineGammaC = 15.5,
            refineGammaP = 8.0,
            refineUseTcOrRcPixSize = false,
            exportIntermediateResults = false,
            nbGPUs = 0,
            verboseLevel = "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/"
        };
    }
}

class DepthMapFilter : Node
{
    public DepthMapFilter() : base("DepthMapFilter", 10, 50, 5)
    {
        inputs = new
        {
            input =  "{DepthMap_1.input}",
            depthMapsFolder = "{DepthMap_1.output}",
            minViewAngle = 2.0,
            maxViewAngle = 70.0,
            nNearestCams = 10,
            minNumOfConsistentCams = 3,
            minNumOfConsistentCamsWithLowSimilarity = 4,
            pixSizeBall = 0,
            pixSizeBallWithLowSimilarity = 0,
            verboseLevel = "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/"
        };
    }
}

class Meshing : Node
{
    public Meshing() : base("Meshing", 0, 1, 1)
    {
        inputs = new
        {
            input = "{DepthMapFilter_1.input}",
            depthMapsFolder = "{DepthMapFilter_1.depthMapsFolder}",
            depthMapsFilterFolder = "{DepthMapFilter_1.output}",
            estimateSpaceFromSfM = true,
            estimateSpaceMinObservations =  3,
            estimateSpaceMinObservationAngle =  10,
            maxInputPoints =  50000000,
            maxPoints =  5000000,
            maxPointsPerVoxel =  1000000,
            minStep =  2,
            partitioning = "singleBlock",
            repartition = "multiResolution",
            angleFactor = 15.0,
            simFactor = 15.0,
            pixSizeMarginInitCoef = 2.0,
            pixSizeMarginFinalCoef = 4.0,
            voteMarginFactor = 4.0,
            contributeMarginFactor = 2.0,
            simGaussianSizeInit = 10.0,
            simGaussianSize = 10.0,
            minAngleThreshold = 1.0,
            refineFuse = true,
            addLandmarksToTheDensePointCloud = false,
            verboseLevel =  "info"
        };

        outputs = new
        {
            output = "{cache}/{nodeType}/{uid0}/mesh.obj",
            outputDenseReconstruction = "{cache}/{nodeType}/{uid0}/denseReconstruction.bin"
        };
    }
}

class PipelineJson
{
    const string MESHROOM_RELASE = "2019.1.0";

    public object header = new {
        pipelineVersion = "2.1",
        releaseVersion = MESHROOM_RELASE,
        fileVersion = "1.1",
        nodesVersions = new
        {
            ImageMatching = "1.0",
            FeatureMatching = "2.0",
            DepthMap = "2.0",
            CameraInit = "2.0",
            DepthMapFilter = "2.0",
            Meshing = "2.0",
            StructureFromMotion = "2.0",
            Texturing = "3.0",
            PrepareDenseScene = "2.0",
            MeshFiltering = "1.0",
            FeatureExtraction = "1.1",
        }
    };

    public Dictionary<string, Node> graph = new Dictionary<string, Node>();

    public PipelineJson(double FocalLength, int Width, int Height, IEnumerable<string> Images,
                        string SensorDatabase, string VocTree,
                        bool CreateMesh = false)
    {
        graph.Add("CameraInit_1",
             new CameraInit(FocalLength, Width, Height, Images, SensorDatabase));
        graph.Add("FeatureExtraction_1", new FeatureExtraction());
        graph.Add("ImageMatching_1", new ImageMatching(VocTree));
        graph.Add("FeatureMatching_1", new FeatureMatching());
        graph.Add("StructureFromMotion_1", new StructureFromMotion());

        if (CreateMesh)
        {
            graph.Add("PrepareDenseScene_1", new PrepareDenseScene());
            graph.Add("DepthMap_1", new DepthMap());
            graph.Add("DepthMapFilter_1", new DepthMapFilter());
            graph.Add("Meshing_1", new Meshing());
        }
    }

    public string Dumps()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public void WriteToFile(string FilePath)
    {
        File.WriteAllText(FilePath, Dumps());
    }
}
#pragma warning restore
