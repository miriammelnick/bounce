/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

// Uncomment this line if you want to use the pattern-based marker tracking
//#define USE_PATTERN_MARKER

// Comment this line to use mono mode
//#define STEREO_MODE

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Generic;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Util;
using GoblinXNA.Helpers;
using GoblinXNA.UI;
using GoblinXNA.UI.UI2D;

using GoblinXNA.Physics;
using GoblinXNA.Physics.Matali;
using Komires.MataliPhysics;
using MataliPhysicsObject = Komires.MataliPhysics.PhysicsObject;
#if WINDOWS_PHONE
using GoblinXNA.Graphics.ParticleEffects2D;
#endif


namespace BounceLib
{
    public class Tutorial8_Phone
    {
        SpriteFont sampleFont;
        SpriteFont uiFont;
        Scene scene;
        MarkerNode groundMarkerNode, toolbarMarkerNode1, toolbarMarkerNode2, resetMarkerNode;
        MarkerNode menuMarkerNode1, menuMarkerNode2, menuMarkerNode3, menuMarkerNode4;
        bool useStaticImage = false;
        bool useSingleMarker = false;
        bool betterFPS = true; // has trade-off of worse tracking if set to true

        LightNode lightNode;
        Viewport viewport;

#if USE_PATTERN_MARKER
        float markerSize = 32.4f;
#else
        float markerSize = 57f;
#endif
        string label = "Nothing is selected";
        Camera camera;

        //geometry and transformation nodes for the objects in the scene
        GeometryNode gameStartNode;
        TransformNode gameStartTransNode;
        GeometryNode gameBoardNode;
        GeometryNode goalNode;
        TransformNode goalTransNode;
        GeometryNode levelNode;
        TransformNode levelTransNode;
        TransformNode levelGoalTransNode;
        GeometryNode levelCompletedNode;
        TransformNode levelCompletedTransNode;
        GeometryNode poleNode;
        TransformNode poleTransNode;
        GeometryNode ballNode;
        TransformNode ballTransNode;
        TransformNode laserGroup;
        TransformNode cancelTransNode, resetTransNode, scaleTransNode, exitTransNode, rotateTransNode, saveTransNode;

        //list of all the level models
        List<Model> levelModelList = new List<Model>();
        List<Model> goalModelList = new List<Model>();
        List<Model> groundModelList = new List<Model>();

        // Scaling and Rotation Variables
        string gameMode = "SHOOTING"; //designates transformation mode, takes on values of "ROTATION" and "SCALING" (and "" for neither)
        string selectedObj = ""; //designates selected object by name
        Vector3 initialSelectedPosition = new Vector3(-1, -1, -1); //stores initial position of selected object

        int currentLevel = 0; //designates the current level the game is on
        int collisionCounter = 0; //counts the number of collisions a ball has made on a single level
        int minNumCollisions = 0; //designates the minimum number of collisions required for a single level
        int levelCompletedRotAngle = 0; //designates rotation angle for level completed icon model
        int gameStartRotAngle = 0; //designates the rotation angle for the game start icon model

        bool ballShot = false;
        string resultlabel = "";
        int countdown = 0;
        int laserNum = 0;

        #region set up stereo viewer
        // The gap on the center between the left and right screen to prevent the left eye
        // seeing the right eye view and the right eye seeing the left eye view
        const int CENTER_GAP = 16; // in pixels

        // The shift amount in pixels from the center of the cropped video image presented to the left eye. 
        const int LEFT_IMAGE_SHIFT_FROM_CENTER = 20; // 20 pixels to the right from its center

        // The shift amount in pixels of the right eye image relative to the center of the cropped image
        // presented to the left eye.
        const int GAP_BETWEEN_LEFT_AND_RIGHT_IMAGE = -40; // 40 pixels to the left

        RenderTarget2D stereoScreenLeft;
        RenderTarget2D stereoScreenRight;
        Rectangle leftRect;
        Rectangle rightRect;
        Rectangle leftSource;
        Rectangle rightSource;

        #endregion

        public Tutorial8_Phone()
        {
            // no contents
        }

        public Texture2D VideoBackground
        {
            get { return scene.BackgroundTexture; }
            set { scene.BackgroundTexture = value; }
        }

        public void Initialize(IGraphicsDeviceService service, ContentManager content, VideoBrush videoBrush)
        {

            viewport = new Viewport(0, 0, 800, 480);
            viewport.MaxDepth = service.GraphicsDevice.Viewport.MaxDepth;
            viewport.MinDepth = service.GraphicsDevice.Viewport.MinDepth;
            service.GraphicsDevice.Viewport = viewport;

            // Initialize the GoblinXNA framework
            State.InitGoblin(service, content, "");

            LoadContent(content);

            //State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            // Initialize the scene graph
            scene = new Scene();
            scene.BackgroundColor = Color.Black;

            scene.PhysicsEngine = new MataliPhysics();

            scene.PhysicsEngine.Gravity = 0f;
            ((MataliPhysics)scene.PhysicsEngine).SimulationTimeStep = 1 / 60f;
            //MouseInput.Instance.MouseClickEvent += new HandleMouseClick(MouseClickHandler);

            State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            // Set up the lights used in the scene
            CreateLights();
#if STEREO_MODE    
            // Set up the stereo camera, which defines the location and viewing frustum of
            // left and right eyes. 
            SetupStereoCamera();
#else
            CreateCamera();
#endif
            // Set up the viewport for rendering stereo view. This function also handles the mono viewport.
            SetupStereoViewport();
 
            SetupMarkerTracking(videoBrush);
            
            CreateObjects(content);

            State.ShowNotifications = true;
            Notifier.Font = sampleFont;

            State.ShowFPS = true;
        }


        private void SetupStereoCamera()
        {
            // Create a stereo camera
            StereoCamera camera = new StereoCamera();
            camera.Translation = new Vector3(0, 0, 0);

            // Set the interpupillary distance which defines the distance between the left
            // and right eyes
#if STEREO_MODE
            camera.InterpupillaryDistance = 5.5f; // 5.5 cm
#else
            camera.InterpupillaryDistance = 20; 
#endif
            // Set the focal distance to be at infinity
            camera.FocalLength = float.MaxValue;

            CameraNode cameraNode = new CameraNode(camera);

            scene.RootNode.AddChild(cameraNode);
            scene.CameraNode = cameraNode;
        }

        private void SetupStereoViewport()
        {
#if STEREO_MODE
            // Since we're doing split-screen stereo rendering, the width for each eye's rendered view
            // will be half of the entire screen
            int stereoWidth = (State.Width - CENTER_GAP) / 2;
            int stereoHeight = State.Height;

            PresentationParameters pp = State.Device.PresentationParameters;

            stereoScreenLeft = new RenderTarget2D(State.Device, stereoWidth, stereoHeight, false,
                SurfaceFormat.Color, pp.DepthStencilFormat);
            stereoScreenRight = new RenderTarget2D(State.Device, stereoWidth, stereoHeight, false,
                SurfaceFormat.Color, pp.DepthStencilFormat);

            leftRect = new Rectangle(0, 0, stereoWidth, stereoHeight);
            rightRect = new Rectangle(stereoWidth + CENTER_GAP, 0, stereoWidth, stereoHeight);

            scene.BackgroundBound = leftRect;
#else
            //// The phone's width is 800, but since we're rendering the video image with aspect ratio of 4x3 
            //// on the background, so we'll hard-code the width to be 640
            //int stereoWidth = 640; 
            //int stereoHeight = State.Height;

            //PresentationParameters pp = State.Device.PresentationParameters;

            //stereoScreenLeft = new RenderTarget2D(State.Device, stereoWidth, stereoHeight, false,
            //    SurfaceFormat.Color, pp.DepthStencilFormat);
            //stereoScreenRight = new RenderTarget2D(State.Device, stereoWidth, stereoHeight, false,
            //    SurfaceFormat.Color, pp.DepthStencilFormat);

            //int screenWidth = 800;

            //leftRect = new Rectangle(0, 0, (screenWidth - CENTER_GAP) / 2, stereoHeight);
            //rightRect = new Rectangle(leftRect.Width + CENTER_GAP, 0, leftRect.Width, stereoHeight);

            //int sourceWidth = (screenWidth - CENTER_GAP) / 2;

            //// We will render half (a little less than half to be exact due to CENTER_GAP) of the 
            //// entire video image for both the left and right eyes, so we need to set the crop
            //// area
            //leftSource = new Rectangle((screenWidth - sourceWidth) / 2 + LEFT_IMAGE_SHIFT_FROM_CENTER, 0, sourceWidth, State.Height);
            //rightSource = new Rectangle(leftSource.X + GAP_BETWEEN_LEFT_AND_RIGHT_IMAGE, 0, sourceWidth, State.Height);
#endif
        }

#if STEREO_MODE
        private void SetupMarkerTracking(VideoBrush videoBrush)
        {
            PhoneCameraCapture captureDevice = new PhoneCameraCapture(videoBrush);
            captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                ImageFormat.B8G8R8A8_32, false);
            ((PhoneCameraCapture)captureDevice).UseLuminance = true;

            if (betterFPS)
                captureDevice.MarkerTrackingImageResizer = new HalfResizer();

            scene.AddVideoCaptureDevice(captureDevice);

            // Use NyARToolkit ID marker tracker
            NyARToolkitIdTracker tracker = new NyARToolkitIdTracker();

            if (captureDevice.MarkerTrackingImageResizer != null)
                tracker.InitTracker((int)(captureDevice.Width * captureDevice.MarkerTrackingImageResizer.ScalingFactor),
                    (int)(captureDevice.Height * captureDevice.MarkerTrackingImageResizer.ScalingFactor),
                    "camera_para.dat");
            else
                tracker.InitTracker(captureDevice.Width, captureDevice.Height, "camera_para.dat");

            // Set the marker tracker to use for our scene
            scene.MarkerTracker = tracker;

            ((StereoCamera)scene.CameraNode.Camera).RightProjection = tracker.CameraProjection;

            // Create a marker node to track a ground marker array.
            groundMarkerNode = new MarkerNode(scene.MarkerTracker, "NyARIdGroundArray.xml", 
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(groundMarkerNode);
        }
#else
        
        private void SetupMarkerTracking(VideoBrush videoBrush)
        {
            IVideoCapture captureDevice = null;

            if (useStaticImage)
            {
                captureDevice = new NullCapture();
                captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._320x240,
                    ImageFormat.B8G8R8A8_32, false);
                if (useSingleMarker)
                    ((NullCapture)captureDevice).StaticImageFile = "MarkerImageHiro.jpg";
                else
                    ((NullCapture)captureDevice).StaticImageFile = "MarkerImage_320x240";

                scene.ShowCameraImage = true;
            }
            else
            {
                captureDevice = new PhoneCameraCapture(videoBrush);
                captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                    ImageFormat.B8G8R8A8_32, false);
                ((PhoneCameraCapture)captureDevice).UseLuminance = true;

                if (betterFPS)
                    captureDevice.MarkerTrackingImageResizer = new HalfResizer();
            }

            // Add this video capture device to the scene so that it can be used for
            // the marker tracker
            scene.AddVideoCaptureDevice(captureDevice);

#if USE_PATTERN_MARKER
            NyARToolkitTracker tracker = new NyARToolkitTracker();
#else
            NyARToolkitIdTracker tracker = new NyARToolkitIdTracker();
#endif

            if (captureDevice.MarkerTrackingImageResizer != null)
                tracker.InitTracker((int)(captureDevice.Width * captureDevice.MarkerTrackingImageResizer.ScalingFactor),
                    (int)(captureDevice.Height * captureDevice.MarkerTrackingImageResizer.ScalingFactor),
                    "camera_para.dat");
            else
                tracker.InitTracker(captureDevice.Width, captureDevice.Height, "camera_para.dat");

            // Set the marker tracker to use for our scene
            scene.MarkerTracker = tracker;


        }
#endif


        private void CreateCamera()
        {
            // Create a camera 
            camera = new Camera();
            // Put the camera at the origin
            camera.Translation = new Vector3(0, 0, 0);
            // Set the vertical field of view to be 60 degrees
            camera.FieldOfViewY = MathHelper.ToRadians(60);
            // Set the near clipping plane to be 0.1f unit away from the camera
            camera.ZNearPlane = 0.1f;
            // Set the far clipping plane to be 1000 units away from the camera
            camera.ZFarPlane = 1000;

            // Now assign this camera to a camera node, and add this camera node to our scene graph
            CameraNode cameraNode = new CameraNode(camera);
            scene.RootNode.AddChild(cameraNode);

            // Assign the camera node to be our scene graph's current camera node
            scene.CameraNode = cameraNode;
        }

        private void CreateLights()
        {
            // Create a directional light source

            LightSource lightSource = new LightSource();
            //lightSource.Position = new Vector3(-50, 50, 100);
            lightSource.Type = LightType.Directional;

            //lightSource.Attenuation0 = 10f;
            //lightSource.Attenuation1 = 5f;
            //lightSource.Attenuation2 = 1f;
            lightSource.Direction = new Vector3(1, -1, -1);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = new Vector4(0.6f, 0.6f, 0.6f, 1);

            // Create a light node to hold the light source
            //LightNode lightNode = new LightNode();
            lightNode = new LightNode();
            lightNode.AmbientLightColor = new Vector4(0.2f, 0.2f, 0.2f, 1);
            lightNode.LightSource = lightSource;

            scene.RootNode.AddChild(lightNode);
        }

        private void CreateObjects(ContentManager content)
        {
            // Create a marker node to track a ground marker array.
#if USE_PATTERN_MARKER
            if(useSingleMarker)
                groundMarkerNode = new MarkerNode(scene.MarkerTracker, "patt.hiro", 16, 16, markerSize, 0.7f);
            else
                groundMarkerNode = new MarkerNode(scene.MarkerTracker, "NyARToolkitGroundArray.xml", 
                    NyARToolkitTracker.ComputationMethod.Average);

#else
            groundMarkerNode = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDGroundArray.xml",
                NyARToolkitTracker.ComputationMethod.Average);
#endif
            scene.RootNode.AddChild(groundMarkerNode);

            // groundMarkerNode.Smoother = new DESSmoother(0.3f, 0.5f);

            toolbarMarkerNode1 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceToolbar.xml",
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(toolbarMarkerNode1);
            toolbarMarkerNode2 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceCue.xml",
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(toolbarMarkerNode2);
            resetMarkerNode = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceReset.xml",
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(resetMarkerNode);

            //groundMarkerNode.AddChild(lightNode);

            // Create a marker node to track each menu array
            menuMarkerNode1 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceMenu1.xml",
                NyARToolkitIdTracker.ComputationMethod.Average);
            menuMarkerNode2 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceMenu2.xml",
                NyARToolkitIdTracker.ComputationMethod.Average);
            menuMarkerNode3 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceMenu3.xml",
                NyARToolkitIdTracker.ComputationMethod.Average);
            menuMarkerNode4 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDBounceMenu4.xml",
                NyARToolkitIdTracker.ComputationMethod.Average);

            scene.RootNode.AddChild(menuMarkerNode1);
            scene.RootNode.AddChild(menuMarkerNode2);
            scene.RootNode.AddChild(menuMarkerNode3);
            scene.RootNode.AddChild(menuMarkerNode4);

            CreateModels();

        }

        private void CreateModels()
        {
            ModelLoader loader = new ModelLoader();
            float scale2 = 0.25f;
            //not sure if necessary to scale, find a good number later
            //if (levelNode != null && levelNode.Model != null )
            //{
            //    //calculate scaling for models to fit nicely in scene
            //    Vector3 dimension = Vector3Helper.GetDimensions(levelNode.Model.MinimumBoundingBox);
            //    scale2 = markerSize/Math.Max(dimension.X, dimension.Z)*5;

            //}

            #region preload all the level, ground and goal models into levelModelList and goalModelList
            for (int i = 1; i <= 4; i++)
            {
                levelModelList.Add((Model)loader.Load("", String.Format("bouncelevel{0}panels", i)));
                goalModelList.Add((Model)loader.Load("", String.Format("goallevel{0}", i)));
            }
            //load 11 and 15, gotta fix the names if we're not adding more levels
            levelModelList.Add((Model)loader.Load("", String.Format("bouncelevel{0}panels", 11)));
            goalModelList.Add((Model)loader.Load("", String.Format("goallevel{0}", 11)));
            levelModelList.Add((Model)loader.Load("", String.Format("bouncelevel{0}panels", 15)));
            goalModelList.Add((Model)loader.Load("", String.Format("goallevel{0}", 15)));
            #endregion
            
            #region load the game start icon model
            gameStartNode = new GeometryNode("gameStartModel");
            gameStartNode.Model = (Model)loader.Load("", "BounceStartScene");
            ((Model)gameStartNode.Model).UseInternalMaterials = true;

            gameStartTransNode = new TransformNode();
            TransformNode gameStartTransNode1 = new TransformNode() //apply initial rotation and scaling
            {
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };

            gameStartNode.Physics = new MataliObject(gameStartNode);
            gameStartNode.Physics.MaterialName = "gameStartModel";
            gameStartNode.Physics.Shape = GoblinXNA.Physics.ShapeType.TriangleMesh;
            gameStartNode.Physics.Collidable = true;
            gameStartNode.Physics.Mass = 100f;
            gameStartNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(gameStartTransNode);
            gameStartTransNode.AddChild(gameStartTransNode1);
            gameStartTransNode1.AddChild(gameStartNode);
            //gameStartNode.Enabled = false;
            #endregion

            #region load the model for the game board
            gameBoardNode = new GeometryNode("gameBoardModel");
            gameBoardNode.Model = (Model)loader.Load("", "Gameboard");
            ((Model)gameBoardNode.Model).UseInternalMaterials = true;

            TransformNode gameBoardTransNode1 = new TransformNode()
            {
                //Translation = new Vector3(0, 0, -100)
            };
            TransformNode gameBoardTransNode = new TransformNode()
            {
                Translation = new Vector3(10000, 10000, 10000),
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };

            gameBoardNode.Physics = new MataliObject(gameBoardNode);
            gameBoardNode.Physics.MaterialName = "gameBoardModel";
            gameBoardNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            gameBoardNode.Physics.Collidable = true;
            gameBoardNode.Physics.Mass = 100f;
            gameBoardNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(gameBoardTransNode1);
            gameBoardTransNode1.AddChild(gameBoardTransNode);
            gameBoardTransNode.AddChild(gameBoardNode);
            //gameBoardNode.Enabled = false; //initially disabled at game start
            #endregion

            /* Game levels models will be swapped by interchanging the models for the levelNode geometry node.
             * The goalNode level will be swapped for the corresponding goal model as well.
             * General conventions:
             * - level models will be named bouncelevel<X>panels where <X> is the level
             * - the goal model corresponding to each level will be named goallevel<X>
             */
            #region load the model for the level
            levelNode = new GeometryNode("levelModel");
            levelNode.Model = levelModelList[0];
            ((Model)levelNode.Model).UseInternalMaterials = true;

            levelTransNode = new TransformNode()
            {
                Translation = new Vector3(10000, 10000, 10000)
            };
            TransformNode levelTransNode1 = new TransformNode()
            {
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };

            levelNode.Physics = new MataliObject(levelNode);
            levelNode.Physics.MaterialName = "levelModel";
            levelNode.Physics.Shape = GoblinXNA.Physics.ShapeType.TriangleMesh;
            levelNode.Physics.Collidable = true;
            levelNode.Physics.Mass = 100f;
            levelNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(levelTransNode);
            levelTransNode.AddChild(levelTransNode1);
            levelTransNode1.AddChild(levelNode);
            //levelNode.Enabled = false; //initially disabled at game start
            #endregion

            #region load model for goal detection
            goalNode = new GeometryNode("goalModel");
            goalNode.Model = new Sphere(60, 16, 16);
            //((Model)goalNode.Model).UseInternalMaterials = true;

            Material goalMat = new Material();
            goalMat.Diffuse = Color.Red.ToVector4();
            goalNode.Material = goalMat;

            //our goal models were causing a lot of lag when being loaded as
            //triangle meshes and causing inaccurate physics engine issues with collision when
            //loaded otherwise so we had to create a simple sphere and then position it
            //where the model was originally
            goalTransNode = new TransformNode()
            {
                Translation = new Vector3(10000, 10000, 10000)
            };
            TransformNode goalTransNode2 = new TransformNode()
            {
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };
            TransformNode goalTransNode1 = new TransformNode()
            {
                Translation = goalModelList[0].MinimumBoundingSphere.Center
            };

            goalNode.Physics = new MataliObject(goalNode);
            goalNode.Physics.MaterialName = "goalModel";
            goalNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;
            goalNode.Physics.Collidable = true;
            goalNode.Physics.Mass = 100f;
            goalNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(goalTransNode);
            goalTransNode.AddChild(goalTransNode2);
            goalTransNode2.AddChild(goalTransNode1);
            goalTransNode1.AddChild(goalNode);
            //goalNode.Enabled = false; //initially disabled at game start
            #endregion

            #region load the cue stick model
            poleNode = new GeometryNode("poleModel");
            poleNode.Model = (Model)loader.Load("", "Cuesmall");
            ((Model)poleNode.Model).UseInternalMaterials = true;

            poleTransNode = new TransformNode();

            poleNode.Physics = new MataliObject(poleNode);
            ((MataliObject)poleNode.Physics).CollisionStartCallback = ToolBar1CollideWithObject;
            poleNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            poleNode.Physics.Collidable = true;
            poleNode.Physics.Mass = 100f;
            poleNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(poleTransNode);
            poleTransNode.AddChild(poleNode);
            poleNode.Enabled = false; //disable if there is no toolbar node visible in the scene
            #endregion

            #region load the ball model
            //ballNode = new GeometryNode("ballModel");
            //ballNode.Model = new Sphere(12, 16, 16);// (Model)loader.Load("", "Ball");
            ////((Model)ballNode.Model).UseInternalMaterials = true;

            //Material ballMat = new Material();
            //ballMat.Diffuse = Color.Green.ToVector4();
            //ballNode.Material = ballMat;

            //ballTransNode = new TransformNode();
            ////ballTransNode.Scale = new Vector3(scale2, scale2, scale2);
            ////ballTransNode.Translation = new Vector3(0, 75, 0);
            ////ballTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize + 75, 0);

            //ballNode.Physics = new MataliObject(ballNode);
            //((MataliObject)ballNode.Physics).CollisionStartCallback = BallCollideWithObject;
            //((MataliObject)ballNode.Physics).Restitution = 1f;
            //ballNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;
            //ballNode.Physics.Interactable = true;
            //ballNode.Physics.Collidable = true;
            //ballNode.Physics.Mass = 10f;
            //ballNode.AddToPhysicsEngine = true;

            //groundMarkerNode.AddChild(ballTransNode);
            //ballTransNode.AddChild(ballNode);
            //ballNode.Enabled = false; //initially disabled at game start 
            #endregion

            #region load the model for level transition after completion of a level
            levelCompletedNode = new GeometryNode("levelCompletedModel");
            levelCompletedNode.Model = (Model)loader.Load("", "Youwon");
            ((Model)levelCompletedNode.Model).UseInternalMaterials = true;

            levelCompletedTransNode = new TransformNode()
            {
                Translation = new Vector3(10000, 10000, 10000)
            };
            TransformNode levelCompletedTransNode1 = new TransformNode()
            {
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };

            levelCompletedNode.Physics = new MataliObject(levelCompletedNode);
            levelCompletedNode.Physics.MaterialName = "levelCompletedModel";
            levelCompletedNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            levelCompletedNode.Physics.Collidable = true;
            levelCompletedNode.Physics.Mass = 100f;
            levelCompletedNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(levelCompletedTransNode);
            levelCompletedTransNode.AddChild(levelCompletedTransNode1);
            levelCompletedTransNode1.AddChild(levelCompletedNode);
            //levelCompletedNode.Enabled = false; //disable at first, enable once a level has ended
            #endregion

            laserGroup = new TransformNode();
            groundMarkerNode.AddChild(laserGroup);

            #region load the menu buttons
            // Add Rotate button to menu
            GeometryNode rotateNode = new GeometryNode("Rotate");
            rotateNode.Model = (Model)loader.Load("", "Rsign");
            ((Model)rotateNode.Model).UseInternalMaterials = true;
            rotateTransNode = new TransformNode
            {
                Translation = new Vector3(-30, 80, 0f),
                Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(270), MathHelper.ToRadians(180), MathHelper.ToRadians(105)),
                Scale = new Vector3(0.1f, 0.1f, 0.1f)
            };

            menuMarkerNode1.AddChild(rotateTransNode);
            rotateTransNode.AddChild(rotateNode);


            // Add Scale button to menu
            GeometryNode scaleNode = new GeometryNode("Scale");
            scaleNode.Model = (Model)loader.Load("", "size");
            ((Model)scaleNode.Model).UseInternalMaterials = true;
            scaleTransNode = new TransformNode
            {
                Translation = new Vector3(-30, 80, 0f),
                Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(330), MathHelper.ToRadians(315), MathHelper.ToRadians(120)),
                Scale = new Vector3(0.3f, 0.3f, 0.3f)
            };

            menuMarkerNode2.AddChild(scaleTransNode);
            scaleTransNode.AddChild(scaleNode);
            
            // Add Reset button to menu
            GeometryNode resetNode = new GeometryNode("Reset");
            resetNode.Model = (Model)loader.Load("", "Reset2");
            ((Model)resetNode.Model).UseInternalMaterials = true;
            resetTransNode = new TransformNode()
            {
                Translation = new Vector3(-30, 80, 0),
                Scale = new Vector3(0.2f, 0.2f, 0.2f)

            };

            menuMarkerNode3.AddChild(resetTransNode);
            resetTransNode.AddChild(resetNode);

            // Add Exit button to menu
            GeometryNode exitNode = new GeometryNode("Exit");
            exitNode.Model = (Model)loader.Load("", "Exit");
            ((Model)exitNode.Model).UseInternalMaterials = true;
            exitTransNode = new TransformNode()
            {
                Translation = new Vector3(-30, 80, 0),
                Scale = new Vector3(0.2f, 0.2f, 0.2f)
            };

            menuMarkerNode4.AddChild(exitTransNode);
            exitTransNode.AddChild(exitNode);


            // Add Cancel button to menu
            GeometryNode cancelNode = new GeometryNode("Cancel");
            cancelNode.Model = (Model)loader.Load("", "Cancel");
            ((Model)cancelNode.Model).UseInternalMaterials = true;
            cancelTransNode = new TransformNode()
            {
                Translation = new Vector3(-30, 80, 0),
                Scale = new Vector3(0.2f, 0.2f, 0.2f)
            };

            //menuMarkerNode1.AddChild(cancelTransNode);
            cancelTransNode.AddChild(cancelNode);


            // Add Save button to menu
            GeometryNode saveNode = new GeometryNode("Save");
            saveNode.Model = (Model)loader.Load("", "Save");
            ((Model)saveNode.Model).UseInternalMaterials = true;
            saveTransNode = new TransformNode()
            {
                Translation = new Vector3(-30, 80, 0),
                Scale = new Vector3(0.2f, 0.2f, 0.2f)
            };

            //menuMarkerNode3.AddChild(saveTransNode);
            saveTransNode.AddChild(saveNode);

            #endregion
            /*
#if WINDOWS_PHONE
            FireParticleEffect fireParticles = new FireParticleEffect(50);
#endif

            ParticleNode ballFireNode = new ParticleNode();
            ballFireNode.ParticleEffects.Add(fireParticles);
            ballFireNode.UpdateHandler += new ParticleUpdateHandler(UpdateFire);
            ballNode.AddChild(ballFireNode);
            */

        }

        private void UpdateFire(Matrix worldTransform, List<ParticleEffect> particleEffects)
        {

            foreach (ParticleEffect particle in particleEffects)
            {
                // Add different number of fire particles based on the position of the ship
                // model. We want to add more particles when it goes through the torus model
                // and decrease the number after that
                int numParticles = 0;
                
                #if WINDOWS_PHONE
                int maxFireParticles = 2;
                #else
                int maxFireParticles = 8;
                #endif
                
                if (particle is FireParticleEffect)
                    numParticles = maxFireParticles;
                else
                    numParticles = 1;

                Matrix world =  worldTransform * groundMarkerNode.WorldTransformation;
                #if WINDOWS_PHONE
                for (int i = 0; i < (numParticles + 2) / 3; i++)
                    particle.AddParticles(Project(world.Translation));

                #else
                for(int i = 0; i < numParticles; i++)
                    particle.AddParticle(worldTransform.Translation + worldTransform.Forward * 1000, 
                        Vector3.Zero);
                #endif
            }

        }

        private void ToolBar1CollideWithObject(MataliPhysicsObject baseObject, MataliPhysicsObject collidingObject)
        {
            string materialName = ((IPhysicsObject)collidingObject.UserTagObj).MaterialName;
            
            switch (materialName)
            {
                case "levelModel":
                    if (gameMode == "ROTATION" || gameMode == "SCALING")
                    {
                        Notifier.AddMessage("Selected the level panels!");

                        //record initial position of the cue marker
                        initialSelectedPosition = poleNode.WorldTransformation.Translation;
                        selectedObj = "levelModel";
                    }
                    break;
            }
        }

        /* Method to handle the collision of the ball and scene objects. If the colliding object is the goal node and
         * the minimum number of collisions has been met, swap to level completion notification model. If the colliding 
         * object is a panel, increment the collision counter. 
         */
        private void BallCollideWithObject(MataliPhysicsObject baseObject, MataliPhysicsObject collidingObject)
        {
            string materialName = ((IPhysicsObject)collidingObject.UserTagObj).MaterialName;

            Matrix removalMat = Matrix.CreateTranslation(new Vector3(10000, 10000, 10000));
            Matrix addMat = Matrix.CreateTranslation(Vector3.Zero); // (new Vector3(-10000, -10000, -10000));
            Vector3 removalVect = new Vector3(10000, 10000, 10000);
            Vector3 addVect = Vector3.Zero;

            switch (materialName)
            {
                case "gameStartModel":
                    //increment the current level to 1
                    Notifier.AddMessage("hit game start!");
                    currentLevel++;

                    laserNum = 0;
                    laserGroup.RemoveChildren();

                    //gameStartNode.Enabled = false;
                    gameStartTransNode.Translation = removalVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(gameStartNode.Physics, removalMat);
                    //scene.PhysicsEngine.RemovePhysicsObject(gameStartNode.Physics);
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)gameStartNode.Physics).EnableCollisions = false;
                    
                    //((TransformNode)gameStartNode.Parent).Translation = removalVect;
                    //((MataliPhysics)scene.PhysicsEngine).SetTransform(gameStartNode.Physics, removalMat);

                    //gameBoardNode.Enabled = true;
                    //scene.GetNode("gameBoardModel").Enabled = true;
                    ((TransformNode)gameBoardNode.Parent).Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(gameBoardNode.Physics, addMat);
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)gameBoardNode.Physics).EnableCollisions = true;
                    //((TransformNode)gameBoardNode.Parent).Translation = addVect;
                    

                    //levelNode.Enabled = true;
                    //scene.GetNode("levelModel").Enabled = true;
                    levelTransNode.Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(levelNode.Physics, addMat);
                    //groundMarkerNode.AddChild(levelGoalTransNode);
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)levelNode.Physics).EnableCollisions = true;
                    //((TransformNode)levelNode.Parent).Translation = addVect;

                    goalTransNode.Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(goalNode.Physics, addMat);
                    //goalNode.Enabled = true;
                    //scene.GetNode("goalModel").Enabled = true;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)goalNode.Physics).EnableCollisions = true;
                    //((TransformNode)goalNode.Parent).Translation = new Vector3(400, 300, 100);
                    //((MataliPhysics)scene.PhysicsEngine).SetTransform(goalNode.Physics, Matrix.CreateTranslation(50, 500, 100));
                    break;
                case "levelModel":
                    Notifier.AddMessage("Hit a panel!");

                    //upon collision with a panel, increment mass to use as a collision counter
                    ((IPhysicsObject)collidingObject.UserTagObj).Mass += 1;
                    break;
                case "goalModel":
                    if (((IPhysicsObject)collidingObject.UserTagObj).Mass < minNumCollisions)
                        break;

                    Notifier.AddMessage(String.Format("You've completed level {0}!", currentLevel));

                    //remove the laser
                    laserNum = 0;
                    laserGroup.RemoveChildren();
                    //scene.PhysicsEngine.RestartsSimulation();

                    //swap transition model for level panels model
                    //levelCompletedNode.Enabled = true;
                    levelCompletedTransNode.Translation = addVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)levelCompletedNode.Physics).EnableCollisions = true;
                    //((TransformNode)levelCompletedNode.Parent).Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(levelCompletedNode.Physics, addMat);

                    //gameBoardNode.Enabled = false;
                    ((TransformNode)gameBoardNode.Parent).Translation = removalVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)gameBoardNode.Physics).EnableCollisions = false;
                    //((TransformNode)gameBoardNode.Parent).Translation = removalVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(gameBoardNode.Physics, removalMat);

                    //levelNode.Enabled = false;
                    levelTransNode.Translation = removalVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)levelNode.Physics).EnableCollisions = false;
                    //((TransformNode)levelNode.Parent).Translation = removalVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(levelNode.Physics, removalMat);

                    goalTransNode.Translation = removalVect;
                    ////goalNode.Enabled = false;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)goalNode.Physics).EnableCollisions = false;
                    ////((TransformNode)goalNode.Parent).Translation = removalVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(goalNode.Physics, removalMat);
                    break;
                case "levelCompletedModel":
                    Notifier.AddMessage(String.Format("Moving on to level {0}...", currentLevel));

                    //increment the current level
                    if (currentLevel < 6)
                        currentLevel++;

                    laserNum = 0;
                    laserGroup.RemoveChildren();

                    //update the panels and goal models for the next level
                    ((MataliPhysics)scene.PhysicsEngine).RemovePhysicsObject((MataliObject)levelNode.Physics);
                    levelNode.Model = levelModelList[currentLevel - 1];
                    ((Model)levelNode.Model).UseInternalMaterials = true;
                    ((MataliPhysics)scene.PhysicsEngine).AddPhysicsObject((MataliObject)levelNode.Physics);//.Model = levelModelList[currentLevel - 1];
                    
                    ((TransformNode)goalNode.Parent).Translation = goalModelList[currentLevel - 1].MinimumBoundingSphere.Center;
                    ((MataliPhysics)scene.PhysicsEngine).SetPosition(goalNode.Physics, goalTransNode.WorldTransformation.Translation);
                    
                    //swap level panels model for level transition model
                    //levelCompletedNode.Enabled = false;
                    levelCompletedTransNode.Translation = removalVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)levelCompletedNode.Physics).EnableCollisions = false;
                    //((TransformNode)levelCompletedNode.Parent).Translation = removalVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(levelCompletedNode.Physics, removalMat);

                    //gameBoardNode.Enabled = true;
                    ((TransformNode)gameBoardNode.Parent).Translation = addVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)gameBoardNode.Physics).EnableCollisions = true;
                    //((TransformNode)gameBoardNode.Parent).Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(gameBoardNode.Physics, addMat);

                    //levelNode.Enabled = true;
                    levelTransNode.Translation = addVect;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)levelNode.Physics).EnableCollisions = true;
                    //((TransformNode)levelNode.Parent).Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(levelNode.Physics, addMat);

                    //goalNode.Enabled = true;
                    //((MataliPhysics)scene.PhysicsEngine).GetMataliPhysicsObject((MataliObject)goalNode.Physics).EnableCollisions = true;
                    //((TransformNode)goalNode.Parent).Translation = addVect;
                    goalTransNode.Translation = addVect;
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(goalNode.Physics, addMat);
                    break;
            }
        }

        private void createLaser()
        {
            if (laserNum > 5){
                laserGroup.RemoveChildAt(0);                
            }

            laserNum++;

            GeometryNode laserNode = new GeometryNode("ball");
            laserNode.Model = new Sphere(5, 20, 20);

            // Create a material to apply to the box model
            Material laserMaterial = new Material();
            laserMaterial.Diffuse = new Vector4(0f, 0.5f, 0, 1);
            laserMaterial.Specular = Color.White.ToVector4();
            laserMaterial.SpecularPower = 10;
            //boxMaterial.Texture = content.Load<Texture2D>("wood");

            laserNode.Material = laserMaterial;

            laserNode.Physics = new MataliObject(laserNode);
            ((MataliObject)laserNode.Physics).Restitution = 1f;
            laserNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;

            laserNode.Physics = new MataliObject(laserNode);
            ((MataliObject)laserNode.Physics).CollisionEndCallback = BallCollideWithObject;
            laserNode.Physics.Interactable = true;
            laserNode.Physics.Collidable = true;
            laserNode.Physics.Mass = 0.0f;

            TransformNode laserTransNode = new TransformNode();
            laserTransNode.Translation = new Vector3(0, 75, 0);
            laserTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize + 75, 0);

            laserGroup.AddChild(laserTransNode);
            laserTransNode.AddChild(laserNode);

#if WINDOWS_PHONE
            FireParticleEffect fireParticles = new FireParticleEffect(25);
#endif
            fireParticles.MaxAcceleration = 1;
            fireParticles.MaxLifetime = 1;
            fireParticles.MaxInitialSpeed = 1;
            
            ParticleNode ballFireNode = new ParticleNode();
            
            ballFireNode.ParticleEffects.Add(fireParticles);
            ballFireNode.UpdateHandler += new ParticleUpdateHandler(UpdateFire);
            laserNode.AddChild(ballFireNode);

            if (toolbarMarkerNode1.MarkerFound)
            {
                Vector3 shiftVector2 = new Vector3(-73, 70, 0);
                Matrix ballmat = Matrix.CreateTranslation(shiftVector2) *
                    toolbarMarkerNode1.WorldTransformation *
                    Matrix.Invert(groundMarkerNode.WorldTransformation);
                ((MataliPhysics)scene.PhysicsEngine).SetTransform(laserNode.Physics, ballmat);
                laserTransNode.WorldTransformation = ballmat;

                Vector3 tail, head, linVel;
                tail = Vector3.Transform(new Vector3(0, -150, 0), toolbarMarkerNode1.WorldTransformation);
                tail = Vector3.Transform(tail, Matrix.Invert(groundMarkerNode.WorldTransformation));
                head = Vector3.Transform(new Vector3(0, 100, 0), toolbarMarkerNode1.WorldTransformation);
                head = Vector3.Transform(head, Matrix.Invert(groundMarkerNode.WorldTransformation));
                linVel = head - tail;
                linVel.Normalize();
                linVel *= 2000f;
                laserNode.Physics.InitialLinearVelocity = linVel;
                laserNode.AddToPhysicsEngine = true;

            }

        }

        private Vector2 Project(Vector3 position)
        {
            Vector3 pos2d = State.Device.Viewport.Project(position, State.ProjectionMatrix,
                State.ViewMatrix, Matrix.Identity);
            return new Vector2(pos2d.X, pos2d.Y);
        }

        private void LoadContent(ContentManager content)
        {
            sampleFont = content.Load<SpriteFont>("Sample");
            uiFont = content.Load<SpriteFont>("UIFont");
        }

        public void Dispose()
        {
            scene.Dispose();
        }

        public void Update(TimeSpan elapsedTime, bool isActive)
        {
            scene.Update(elapsedTime, false, isActive);
        }

        public void Draw(TimeSpan elapsedTime)
        {
            #if STEREO_MODE
            // Set the render target to be the left screen render target
            scene.SceneRenderTarget = stereoScreenLeft;
            // Render the scene viewed from the left eye to the left screen render target
            scene.Draw(elapsedTime, false);

            // Set the render target to be the right screen render target
            scene.SceneRenderTarget = stereoScreenRight;
            // Render the scene viewed from the right eye to the right screen render target
            // NOTE: We use the light version of Draw function here for better performance
            scene.RenderScene(false, false);

            // Set the render target to be the default one (frame buffer)
            State.Device.SetRenderTarget(null);
            
            State.Device.Clear(scene.BackgroundColor);

            #else
            State.Device.Viewport = viewport;
            #endif
            // Render the left and right render targets as textures
            State.SharedSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);



            UI2DRenderer.WriteText(new Vector2(10, 10), label, Color.GreenYellow, sampleFont);
            //UI2DRenderer.WriteText(new Vector2(10, 30), ballTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
            UI2DRenderer.WriteText(new Vector2(10, 70), resultlabel, Color.GreenYellow, sampleFont);
            //UI2DRenderer.WriteText(new Vector2(10, 100), ballTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);

            if (toolbarMarkerNode1.MarkerFound)
            {
                poleNode.Enabled = true; //make the toolbar node visible
                //ballNode.Enabled = true;

                Vector3 shiftVector1 = new Vector3(-73, 70, 0);
                Matrix polemat = Matrix.CreateTranslation(shiftVector1) *
                    toolbarMarkerNode1.WorldTransformation *
                    Matrix.Invert(groundMarkerNode.WorldTransformation);
                ((MataliPhysics)scene.PhysicsEngine).SetTransform(poleNode.Physics, polemat);

                //if (!ballShot)
                //{
                //    Vector3 shiftVector2 = new Vector3(-73, 145, 0);
                //    Matrix ballmat = Matrix.CreateTranslation(shiftVector2) *
                //        toolbarMarkerNode1.WorldTransformation *
                //        Matrix.Invert(groundMarkerNode.WorldTransformation);
                //    ((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, ballmat);
                //    //ballTransNode.WorldTransformation = ballmat;
                //}

                #region handle scaling and rotation of the level panels
                switch (selectedObj)
                {
                    case "levelModel":
                        Vector3 cueVector = polemat.Translation - initialSelectedPosition;
                        Vector3 abs_cueVector = new Vector3(Math.Abs(cueVector.X), Math.Abs(cueVector.Y), Math.Abs(cueVector.Z));
                        float scaledLength = abs_cueVector.Length();
                        float direction = 1;
                        if (gameMode == "ROTATION" && scaledLength > 100)
                        {
                            if (abs_cueVector.X > abs_cueVector.Y && abs_cueVector.X > abs_cueVector.Z)
                            {
                                if (cueVector.X < 0)
                                    direction = -1;
                                Matrix rotMat = levelGoalTransNode.WorldTransformation * Matrix.CreateRotationY(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelGoalTransNode.WorldTransformation);
                                levelGoalTransNode.WorldTransformation = rotMat * levelGoalTransNode.WorldTransformation;
                            }
                            else if (abs_cueVector.Y > abs_cueVector.X && abs_cueVector.Y > abs_cueVector.Z)
                            {
                                if (cueVector.Y > 0)
                                    direction = -1;
                                Matrix rotMat = levelGoalTransNode.WorldTransformation * Matrix.CreateRotationX(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelGoalTransNode.WorldTransformation);
                                levelGoalTransNode.WorldTransformation = rotMat * levelGoalTransNode.WorldTransformation;
                            }
                            else if (abs_cueVector.Z > abs_cueVector.X && abs_cueVector.Z > abs_cueVector.Y)
                            {
                                if (cueVector.Z > 0)
                                    direction = -1;
                                Matrix rotMat = levelGoalTransNode.WorldTransformation * Matrix.CreateRotationZ(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelGoalTransNode.WorldTransformation);
                                levelGoalTransNode.WorldTransformation = rotMat * levelGoalTransNode.WorldTransformation;
                            }
                        }
                        else if (gameMode == "SCALING" && scaledLength > 100)
                        {
                            float scale = (float)Math.Pow(abs_cueVector.Length() / 100, 2);
                            levelGoalTransNode.Scale = new Vector3(scale, scale, scale);
                        }
                        break;
                    case "":
                        break;
                }
                #endregion
            }
            else
            {
                selectedObj = "";

                poleNode.Enabled = false;
                //ballNode.Enabled = false;
            }

            if (countdown++ == 15)
            {
                if (toolbarMarkerNode1.MarkerFound && poleNode.Enabled && gameMode == "SHOOTING")
                    createLaser();
                countdown = 0;
            }

            #region reset the ball position if resetMarkerNode is visible, only during the shooting mode of the game
            //if (resetMarkerNode.MarkerFound && gameMode == "SHOOTING")
            //{
            //    ballNode.Physics.InitialLinearVelocity = new Vector3(0, 0, 0);

            //    scene.PhysicsEngine.RestartsSimulation();
            //    ((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0));
            //    ballTransNode.WorldTransformation = Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0);

            //    ballShot = false;
            //    collisionCounter = 0;
            //}
            #endregion

            #region add rotation to the game start icon model for flavor
            if (gameStartNode.Enabled)
            {
                gameStartRotAngle++;
                gameStartTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(gameStartRotAngle));
            }
            #endregion

            #region add rotation to the level completed icon model for flavor
            if (levelCompletedNode.Enabled)
            {
                levelCompletedRotAngle++;
                levelCompletedTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(levelCompletedRotAngle));
            }
            #endregion

            if (toolbarMarkerNode1.MarkerFound && toolbarMarkerNode2.MarkerFound)
            {

                //if (!ballShot)
                //{
                //    Vector3 tail, head, linVel;
                //    tail = Vector3.Transform(new Vector3(0, -150, 0), toolbarMarkerNode1.WorldTransformation);
                //    tail = Vector3.Transform(tail, Matrix.Invert(groundMarkerNode.WorldTransformation));
                //    head = Vector3.Transform(new Vector3(0, 100, 0), toolbarMarkerNode1.WorldTransformation);
                //    head = Vector3.Transform(head, Matrix.Invert(groundMarkerNode.WorldTransformation));
                //    linVel = head - tail;
                //    linVel.Normalize();
                //    linVel *= 2000f;

                //    ballNode.Physics.InitialLinearVelocity = linVel;

                //    laserGroup.RemoveChildren();
                //    laserNum = 0;
                //    scene.PhysicsEngine.RestartsSimulation();

                //    ballShot = true;
                //    label = "Shoot!";

                //}

            }

            #region check for menu item selection
            if (!menuMarkerNode1.MarkerFound && menuMarkerNode2.MarkerFound && menuMarkerNode3.MarkerFound &&
                menuMarkerNode4.MarkerFound)
            {
                // they chose 1st button
                UI2DRenderer.WriteText(new Vector2(10, 10), "1st button selected", Color.Blue, sampleFont);

            }
            else if (menuMarkerNode1.MarkerFound && !menuMarkerNode2.MarkerFound && menuMarkerNode3.MarkerFound &&
              menuMarkerNode4.MarkerFound)
            {
                // they chose 2nd button
                UI2DRenderer.WriteText(new Vector2(10, 10), "2nd button selected", Color.Blue, sampleFont);

            } else if (menuMarkerNode1.MarkerFound && menuMarkerNode2.MarkerFound && !menuMarkerNode3.MarkerFound &&
                menuMarkerNode4.MarkerFound)
            {
                // they chose 3rd button
                UI2DRenderer.WriteText(new Vector2(10, 10), "3rd button selected", Color.Blue, sampleFont);

            }
            else if (menuMarkerNode1.MarkerFound && menuMarkerNode2.MarkerFound && menuMarkerNode3.MarkerFound &&
              !menuMarkerNode4.MarkerFound)
            {
                // they chose 4th button
                UI2DRenderer.WriteText(new Vector2(10, 10), "4th button selected", Color.Blue, sampleFont);

            }
            #endregion
            label = countdown.ToString();

            #if STEREO_MODE
            State.SharedSpriteBatch.Draw(stereoScreenLeft, leftRect, Color.White);
            State.SharedSpriteBatch.Draw(stereoScreenRight, rightRect, Color.White);
            State.SharedSpriteBatch.End();

            #else
            //State.SharedSpriteBatch.Draw(stereoScreenLeft, leftRect, leftSource, Color.White);
            //State.SharedSpriteBatch.Draw(stereoScreenRight, rightRect, rightSource, Color.White);
            State.SharedSpriteBatch.End();
            scene.Draw(elapsedTime, false);
            #endif

        }

        //should delete this eventually
        private void HandleActionPerformed(object source)
        {
            G2DComponent comp = (G2DComponent)source;

            switch (comp.Text)
            {
                case "reset":
                    //ballNode.Physics.InitialLinearVelocity = new Vector3(0, 0, 0);

                    //scene.PhysicsEngine.RestartsSimulation();
                    //((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0));
                    //ballTransNode.WorldTransformation = Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0);

                    ////toolbarMarkerNode1.Update(1 / 30f);

                    ////scene.PhysicsEngine.Update(1 / 30f);

                    //ballShot = false;
                    break;

                case "Rotation":
                    gameMode = "ROTATION";

                    //add code here to change menu items and selector cue

                    Notifier.AddMessage("Rotation Mode Activated");
                    break;

                case "Scaling":
                    gameMode = "SCALING";

                    //add code here to change menu items and selector cue

                    Notifier.AddMessage("Scaling Mode Activated");
                    break;
            }
        }
    }
}