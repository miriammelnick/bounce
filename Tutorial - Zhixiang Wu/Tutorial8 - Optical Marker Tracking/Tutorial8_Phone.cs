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

using System;
using System.Collections.Generic;
using System.Windows.Media;

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

namespace Tutorial8___Optical_Marker_Tracking___PhoneLib
{
    public class Tutorial8_Phone
    {
        SpriteFont sampleFont;
        SpriteFont uiFont;
        Scene scene;
        MarkerNode groundMarkerNode, toolbarMarkerNode1, toolbarMarkerNode2, toolbarMarkerNode3;
        bool useStaticImage = false;
        bool useSingleMarker = false;
        bool betterFPS = true; // has trade-off of worse tracking if set to true


        LightNode lightNode;
        Viewport viewport;

        TransformNode boxTransNode;
        TransformNode cylinderTransNode;

#if USE_PATTERN_MARKER
        float markerSize = 32.4f;
#else
        float markerSize = 57f;
#endif
        string label = "Nothing is selected";
        GeometryNode boxNode;
        GeometryNode cylinderNode;
        Camera camera;
        Vector3 pickedPosition = Vector3.Zero;

        GeometryNode levelNode;
        TransformNode levelTransNode;
        TransformNode poleTransNode;
        GeometryNode poleNode;
        TransformNode ballTransNode;
        GeometryNode ballNode;

        /* Scaling and Rotation Variables */
        string transMode = "ROTATION"; //designates transformation mode, takes on values of "ROTATION" and "SCALING"
        string selectedObj = ""; //designates selected object by name
        Vector3 initialSelectedPosition = new Vector3(-1, -1, -1); //stores initial position of selected object

        bool ball_on = false;

        List<Vector3> panels = new List<Vector3>();
        int panelsnum = 0;
        GeometryNode[] laserNode = new GeometryNode[10];
        TransformNode[] laserTransNode = new TransformNode[10];
        int lasernum = 0;
        string resultlabel = "";

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
            ((MataliPhysics)scene.PhysicsEngine).SimulationTimeStep = 1 / 30f;
            //MouseInput.Instance.MouseClickEvent += new HandleMouseClick(MouseClickHandler);

            State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            CreateTags(content);
            //CreateTags1(content);
            //CreateTags2(content);
            //CreateTags3(content);
            //CreateTags4(content);

            // Set up the lights used in the scene
            CreateLights();

            CreateCamera();

            SetupMarkerTracking(videoBrush);

            CreateObjects(content);

            State.ShowNotifications = true;
            Notifier.Font = sampleFont;

            State.ShowFPS = true;
        }

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

            toolbarMarkerNode1 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDToolbar3.xml",
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(toolbarMarkerNode1);
            toolbarMarkerNode2 = new MarkerNode(scene.MarkerTracker, "NyARToolkitIDToolbar4.xml",
                NyARToolkitTracker.ComputationMethod.Average);
            scene.RootNode.AddChild(toolbarMarkerNode2);

            //groundMarkerNode.AddChild(lightNode);

            // Create the floor
            GeometryNode groundfloor = new GeometryNode("floor");
            groundfloor.Model = new Box(markerSize, markerSize, 2);
            Material floorMaterial = new Material();
            floorMaterial.Diffuse = new Vector4(0.8f, 0.8f, 0.5f, 1);
            floorMaterial.Specular = Color.White.ToVector4();
            floorMaterial.SpecularPower = 10;
            groundfloor.Material = floorMaterial;
            groundfloor.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            //groundfloor.Physics.Collidable = true;
            //groundfloor.AddToPhysicsEngine = true;

            TransformNode floorTransNode = new TransformNode();
            floorTransNode.Translation = new Vector3(0, 0, -100);
            floorTransNode.Scale = new Vector3(10, 10, 1);
            // Add this box model node to the ground marker node
            //groundMarkerNode.AddChild(floorTransNode);
            //floorTransNode.AddChild(groundfloor);

            boxNode = new GeometryNode("Box");
            boxNode.Model = new Box(50, 5, 50);

            // Create a material to apply to the box model
            Material boxMaterial = new Material();
            boxMaterial.Diffuse = new Vector4(0.5f, 0, 0, 1);
            boxMaterial.Specular = Color.White.ToVector4();
            boxMaterial.SpecularPower = 10;
            //boxMaterial.Texture = content.Load<Texture2D>("wood");

            boxNode.Material = boxMaterial;

            boxNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            boxNode.Physics.Collidable = true;
            boxNode.Physics.Mass = 10f;
            //boxNode.Physics.LinearDamping = 100f;
            //boxNode.Physics.Interactable = true;
            //boxNode.Physics.Pickable = true;
            boxNode.AddToPhysicsEngine = true;

            boxTransNode = new TransformNode();
            boxTransNode.Translation = new Vector3(0, -markerSize * 2, 0);
            // Add this box model node to the ground marker node
            groundMarkerNode.AddChild(boxTransNode);
            boxTransNode.AddChild(boxNode);

            panels.Add(new Vector3(0 - 25, -markerSize * 2, 0 - 25));
            panels.Add(new Vector3(0 - 25, -markerSize * 2, 0 + 25));
            panels.Add(new Vector3(0 + 25, -markerSize * 2, 0 - 25));
            panels.Add(new Vector3(0, 1, 0));
            panelsnum++;

            // Create a Cylinder
            cylinderNode = new GeometryNode("cylinder");
            cylinderNode.Model = new Box(50, 5, 50);

            Material cylinderMaterial = new Material();
            cylinderMaterial.Diffuse = new Vector4(0f, 0, 0.5f, 1);
            boxMaterial.Specular = Color.White.ToVector4();
            cylinderMaterial.SpecularPower = 10;
            //cylinderMaterial.Texture = content.Load<Texture2D>("wood");
            cylinderNode.Material = cylinderMaterial;

            cylinderNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            cylinderNode.Physics.Collidable = true;
            cylinderNode.Physics.Mass = 10f;
            //cylinderNode.Physics.Pickable = true;
            cylinderNode.AddToPhysicsEngine = true;

            cylinderTransNode = new TransformNode();
            cylinderTransNode.Translation = new Vector3(-2 * markerSize, 2 * markerSize, 0);

            groundMarkerNode.AddChild(cylinderTransNode);
            cylinderTransNode.AddChild(cylinderNode);

            CreateModels();

        }

        private void CreateModels()
        {
            ModelLoader loader = new ModelLoader();


            levelNode = new GeometryNode("level1");
            levelNode.Model = (Model)loader.Load("", "bouncelevel1panels");
            ((Model)levelNode.Model).UseInternalMaterials = true;
            Vector3 dimension = Vector3Helper.GetDimensions(levelNode.Model.MinimumBoundingBox);
            float scale2 = markerSize / Math.Max(dimension.X, dimension.Z) * 5;
            levelTransNode = new TransformNode()
            {
                Translation = new Vector3(0, 0, 0),
                //Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90)) *
                //         Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(90)),
                Scale = new Vector3(scale2, scale2, scale2)
            };

            levelNode.Physics.MaterialName = "level1";
            levelNode.Physics.Shape = GoblinXNA.Physics.ShapeType.TriangleMesh;
            levelNode.Physics.Collidable = true;
            levelNode.Physics.Mass = 100f;
            levelNode.AddToPhysicsEngine = true;

            groundMarkerNode.AddChild(levelTransNode);
            levelTransNode.AddChild(levelNode);



            poleNode = new GeometryNode("Pole");
            poleNode.Model = new Box(20, 100, 20);

            // Create a material to apply to the box model
            Material boxMaterial = new Material();
            boxMaterial.Diffuse = new Vector4(0.5f, 0, 0, 1);
            boxMaterial.Specular = Color.White.ToVector4();
            boxMaterial.SpecularPower = 10;
            //boxMaterial.Texture = content.Load<Texture2D>("wood");

            poleNode.Material = boxMaterial;

            poleNode.Physics = new MataliObject(poleNode);
            poleNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;

            //poleNode.Physics.Interactable = true;
            //craftNode.Physics.Pickable = true;
            poleNode.Physics.Collidable = true;
            poleNode.Physics.Mass = 100f;

            ((MataliObject)poleNode.Physics).CollisionStartCallback = ToolBar1CollideWithObject;

            //craftNode.Physics.InitialLinearVelocity = Vector3.Zero;
            poleNode.AddToPhysicsEngine = true;

            poleTransNode = new TransformNode();
            poleTransNode.Translation = new Vector3(0, 0, 0);
            poleTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize, 0);
            //poleTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90));

            groundMarkerNode.AddChild(poleTransNode);
            //toolbarMarkerNode1.AddChild(poleTransNode);
            poleTransNode.AddChild(poleNode);

            ballNode = new GeometryNode("Ball");
            ballNode.Model = new Sphere(15, 20, 20);

            // Create a material to apply to the box model
            Material ballMaterial = new Material();
            ballMaterial.Diffuse = new Vector4(0f, 0.5f, 0, 1);
            ballMaterial.Specular = Color.White.ToVector4();
            ballMaterial.SpecularPower = 10;
            //boxMaterial.Texture = content.Load<Texture2D>("wood");

            ballNode.Material = ballMaterial;

            ballNode.Physics = new MataliObject(ballNode);
            ((MataliObject)ballNode.Physics).Restitution = 1f;
            ballNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;

            ballNode.Physics.Interactable = true;
            //craftNode.Physics.Pickable = true;
            ballNode.Physics.Collidable = true;
            ballNode.Physics.Mass = 10f;
            //ballNode.Physics.InitialLinearVelocity = Vector3.Zero;
            ballNode.AddToPhysicsEngine = true;

            ballTransNode = new TransformNode();
            ballTransNode.Translation = new Vector3(0, 75, 0);
            ballTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize + 75, 0);

            groundMarkerNode.AddChild(ballTransNode);
            //toolbarMarkerNode1.AddChild(ballTransNode);
            ballTransNode.AddChild(ballNode);

            /*
            #if WINDOWS_PHONE
            FireParticleEffect fireParticles = new FireParticleEffect(40);
            #endif

            ParticleNode ballFireNode = new ParticleNode();
            ballFireNode.ParticleEffects.Add(fireParticles);
            //ballFireNode.UpdateHandler += new ParticleUpdateHandler(UpdateShipFire);
            ballNode.AddChild(ballFireNode);
            */


            /*
            trace = new GeometryNode("Trace");
            trace.Model = new Cylinder(5f, 5f, 1f, 20);
            Material traceMaterial = new Material();
            traceMaterial.Diffuse = new Vector4(1f, 0f, 0, 1);
            traceMaterial.Specular = Color.White.ToVector4();
            traceMaterial.SpecularPower = 10;
            
            trace.Material = traceMaterial;
            
            traceNode = new TransformNode();
            traceNode.Translation = new Vector3(0, 90f, 0);
            
            toolbarMarkerNode1.AddChild(traceNode);
            traceNode.AddChild(trace);
            */
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
            //label = ballNode.Physics.InitialLinearVelocity.ToString();

            scene.Update(elapsedTime, false, isActive);
        }

        public void Draw(TimeSpan elapsedTime)
        {
            State.Device.Viewport = viewport;

            UI2DRenderer.WriteText(new Vector2(10, 10), label, Color.GreenYellow, sampleFont);
            UI2DRenderer.WriteText(new Vector2(10, 30), ballTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
            UI2DRenderer.WriteText(new Vector2(10, 70), resultlabel, Color.GreenYellow, sampleFont);
            UI2DRenderer.WriteText(new Vector2(10, 100), ballTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);



            if (toolbarMarkerNode1.MarkerFound)
            {
                Vector3 shiftVector1 = new Vector3(0, 0, 0);
                Matrix polemat = Matrix.CreateTranslation(shiftVector1) *
                    toolbarMarkerNode1.WorldTransformation *
                    Matrix.Invert(groundMarkerNode.WorldTransformation);
                ((MataliPhysics)scene.PhysicsEngine).SetTransform(poleNode.Physics, polemat);

                if (!ball_on)
                {
                    Vector3 shiftVector2 = new Vector3(0, 75, 0);
                    Matrix ballmat = Matrix.CreateTranslation(shiftVector2) *
                        toolbarMarkerNode1.WorldTransformation *
                        Matrix.Invert(groundMarkerNode.WorldTransformation);
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, ballmat);
                    ballTransNode.WorldTransformation = ballmat;
                }

                Vector3 tail = Vector3.Transform(new Vector3(0, 0, 0), toolbarMarkerNode1.WorldTransformation);
                //tail = Vector3.Transform(tail, Matrix.Invert(groundMarkerNode.WorldTransformation));
                Vector3 head = Vector3.Transform(new Vector3(0, 75, 0), toolbarMarkerNode1.WorldTransformation);
                //head = Vector3.Transform(head, Matrix.Invert(groundMarkerNode.WorldTransformation));

                //addLaser(head, tail);

                switch (selectedObj)
                {
                    case "level1":
                        Vector3 cueVector = polemat.Translation - initialSelectedPosition;
                        Vector3 abs_cueVector = new Vector3(Math.Abs(cueVector.X), Math.Abs(cueVector.Y), Math.Abs(cueVector.Z));
                        float scaledLength = abs_cueVector.Length();
                        float direction = 1;
                        if (transMode == "ROTATION" && scaledLength > 100)
                        {
                            if (abs_cueVector.X > abs_cueVector.Y && abs_cueVector.X > abs_cueVector.Z)
                            {
                                if (cueVector.X < 0)
                                    direction = -1;
                                Matrix rotMat = levelTransNode.WorldTransformation * Matrix.CreateRotationY(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelTransNode.WorldTransformation);
                                levelTransNode.WorldTransformation = rotMat * levelTransNode.WorldTransformation;
                            }
                            else if (abs_cueVector.Y > abs_cueVector.X && abs_cueVector.Y > abs_cueVector.Z)
                            {
                                if (cueVector.Y > 0)
                                    direction = -1;
                                Matrix rotMat = levelTransNode.WorldTransformation * Matrix.CreateRotationX(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelTransNode.WorldTransformation);
                                levelTransNode.WorldTransformation = rotMat * levelTransNode.WorldTransformation;
                            }
                            else if (abs_cueVector.Z > abs_cueVector.X && abs_cueVector.Z > abs_cueVector.Y)
                            {
                                if (cueVector.Z > 0)
                                    direction = -1;
                                Matrix rotMat = levelTransNode.WorldTransformation * Matrix.CreateRotationZ(direction * MathHelper.ToRadians(3)) * Matrix.Invert(levelTransNode.WorldTransformation);
                                levelTransNode.WorldTransformation = rotMat * levelTransNode.WorldTransformation;
                            }
                        }
                        else if (transMode == "SCALING" && scaledLength > 100)
                        {
                            float scale = (float)Math.Pow(abs_cueVector.Length() / 100, 2);
                            levelTransNode.Scale = new Vector3(scale, scale, scale);
                        }
                        break;
                    case "":
                        break;
                }
            }
            else
            {
                ((MataliPhysics)scene.PhysicsEngine).SetTransform(poleNode.Physics, Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize, 0));
                if (!ball_on)
                {
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0));
                    ballTransNode.WorldTransformation = Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0);
                }

                selectedObj = "";
            }

            if (toolbarMarkerNode1.MarkerFound && toolbarMarkerNode2.MarkerFound)
            {

                if (!ball_on)
                {
                    Vector3 tail, head, linVel;
                    tail = Vector3.Transform(new Vector3(0, -150, 0), toolbarMarkerNode1.WorldTransformation);
                    tail = Vector3.Transform(tail, Matrix.Invert(groundMarkerNode.WorldTransformation));
                    head = Vector3.Transform(new Vector3(0, 100, 0), toolbarMarkerNode1.WorldTransformation);
                    head = Vector3.Transform(head, Matrix.Invert(groundMarkerNode.WorldTransformation));
                    linVel = head - tail;
                    linVel.Normalize();
                    linVel *= 400f;


                    //craftNode.Physics.Interactable = true;

                    /*
                    Vector3 origin = Vector3.Transform(new Vector3(0, 0, 0), groundMarkerNode.WorldTransformation);
                    Vector3 balltrans = Vector3.Transform(new Vector3(0, 75, 0), toolbarMarkerNode1.WorldTransformation);
                    Matrix mat = Matrix.CreateTranslation(new Vector3(0, 75, 0)) * Matrix.Invert(groundMarkerNode.WorldTransformation);
                        
                    toolbarMarkerNode1.RemoveChild(ballTransNode);
                    groundMarkerNode.AddChild(ballTransNode);
                        
                    ballTransNode.Translation = Vector3.Transform(balltrans, Matrix.Invert(groundMarkerNode.WorldTransformation));
                    */

                    //ballTransNode.Translation = balltrans - origin;
                    //ballTransNode.WorldTransformation = mat;
                    //((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, mat);
                    //ballTransNode.Translation = Vector3.Transform(Vector3.Transform(new Vector3(0, 75, 0), toolbarMarkerNode1.WorldTransformation),Matrix.Invert(groundMarkerNode.WorldTransformation));
                    ballNode.Physics.InitialLinearVelocity = linVel;

                    scene.PhysicsEngine.RestartsSimulation();
                    //toolbarMarkerNode1.Update(1 / 30f);

                    //scene.PhysicsEngine.Update(1 / 30f);


                    ball_on = true;
                    label = "Shoot!";

                }

            }


            /*
            Vector3 camPosition;
            camPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
            // camPosition = new Vector3((int)camPosition.X, (int)camPosition.Y, (int)camPosition.Z);

            if (box_is_on)
            {
                if (box_on == "translate")
                {
                    boxTransNode.Translation = boxTranslation + (camPosition - pickedPosition);
                    UI2DRenderer.WriteText(new Vector2(20, 80), boxTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
                }
                if (box_on == "scale")
                {
                    Vector3 camscale = new Vector3((camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f);
                    boxTransNode.Scale = boxScale + camscale;
                    if (boxTransNode.Scale.X > 3)
                        boxTransNode.Scale = new Vector3(3, 3, 3);
                    if (boxTransNode.Scale.X < 0.25)
                        boxTransNode.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                    UI2DRenderer.WriteText(new Vector2(20, 80), boxTransNode.Scale.ToString(), Color.GreenYellow, sampleFont);
                }
                
                if (box_on == "rotatep")
                {
                    boxdegx = (camPosition.Z - pickedPosition.Z) + boxRotatex;
                    if (boxdegx > 90)
                        boxdegx = 90;
                    if (boxdegx < -90)
                        boxdegx = -90;

                    boxTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(boxdegx)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(boxRotate));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "pitch: "+boxdegx.ToString(), Color.GreenYellow, sampleFont);
                }
                if (box_on == "rotate")
                {
                    boxdeg = ( - camPosition.X + pickedPosition.X) * 2 + boxRotate;
                    if (boxdeg > 180)
                        boxdeg = 180;
                    if (boxdeg < -180)
                        boxdeg = -180;
                    boxTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(boxRotatex)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(boxdeg));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "round: " + boxdeg.ToString(), Color.GreenYellow, sampleFont);
                }
            }

            if (cylinder_is_on)
            {
                if (cylinder_on == "translate")
                {
                    cylinderTransNode.Translation = cylinderTranslation + (camPosition - pickedPosition);
                    UI2DRenderer.WriteText(new Vector2(20, 80), cylinderTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
                }
                if (cylinder_on == "scale")
                {
                    Vector3 camscale = new Vector3((camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f);
                    cylinderTransNode.Scale = cylinderScale + camscale;
                    if (cylinderTransNode.Scale.X > 3)
                        cylinderTransNode.Scale = new Vector3(3, 3, 3);
                    if (cylinderTransNode.Scale.X < 0.25)
                        cylinderTransNode.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                    UI2DRenderer.WriteText(new Vector2(20, 80), cylinderTransNode.Scale.ToString(), Color.GreenYellow, sampleFont);
                }

                if (cylinder_on == "rotatep")
                {
                    cylinderdegx = (camPosition.Z - pickedPosition.Z) + cylinderRotatex;
                    if (cylinderdegx > 90)
                        cylinderdegx = 90;
                    if (cylinderdegx < -90)
                        cylinderdegx = -90;

                    cylinderTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(cylinderdegx)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(cylinderRotate));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "pitch: " + cylinderdegx.ToString(), Color.GreenYellow, sampleFont);
                }
                if (cylinder_on == "rotate")
                {
                    cylinderdeg = (-camPosition.X + pickedPosition.X) * 2 + cylinderRotate;
                    if (cylinderdeg > 180)
                        cylinderdeg = 180;
                    if (cylinderdeg < -180)
                        cylinderdeg = -180;
                    cylinderTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(cylinderRotatex)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(cylinderdeg));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "round: " + cylinderdeg.ToString(), Color.GreenYellow, sampleFont);
                }
            }

            if (craft_is_on)
            {
                if (craft_on == "translate")
                {
                    craftTransNode.Translation = craftTranslation + (camPosition - pickedPosition);
                    UI2DRenderer.WriteText(new Vector2(20, 80), craftTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
                }
                if (craft_on == "scale")
                {
                    Vector3 camscale = new Vector3((camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f);
                    craftTransNode.Scale = craftScale + camscale;
                    if (craftTransNode.Scale.X > 3 * craftScale0)
                        craftTransNode.Scale = new Vector3(3, 3, 3);
                    if (craftTransNode.Scale.X < 0.25 * craftScale0)
                        craftTransNode.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                    UI2DRenderer.WriteText(new Vector2(20, 80), craftTransNode.Scale.ToString(), Color.GreenYellow, sampleFont);
                }

                if (craft_on == "rotatep")
                {
                    craftdegx = (camPosition.Z - pickedPosition.Z) + craftRotatex;
                    if (craftdegx > 90)
                        craftdegx = 90;
                    if (craftdegx < -90)
                        craftdegx = -90;

                    craftTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(craftdegx)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(craftRotate));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "pitch: " + craftdegx.ToString(), Color.GreenYellow, sampleFont);
                }
                if (craft_on == "rotate")
                {
                    craftdeg = (-camPosition.X + pickedPosition.X) * 2 + craftRotate;
                    if (craftdeg > 180)
                        craftdeg = 180;
                    if (craftdeg < -180)
                        craftdeg = -180;
                    craftTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(craftRotatex)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(craftdeg));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "round: " + craftdeg.ToString(), Color.GreenYellow, sampleFont);
                }
            }

            if (tower_is_on)
            {
                if (tower_on == "translate")
                {
                    towerTransNode.Translation = towerTranslation + (camPosition - pickedPosition);
                    UI2DRenderer.WriteText(new Vector2(20, 80), towerTransNode.Translation.ToString(), Color.GreenYellow, sampleFont);
                }
                if (tower_on == "scale")
                {
                    Vector3 camscale = new Vector3((camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f, (camPosition.Y - pickedPosition.Y) / 50.0f);
                    towerTransNode.Scale = towerScale + camscale;
                    if (towerTransNode.Scale.X > 3)
                        towerTransNode.Scale = new Vector3(3, 3, 3);
                    if (towerTransNode.Scale.X < 0.25)
                        towerTransNode.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                    UI2DRenderer.WriteText(new Vector2(20, 80), towerTransNode.Scale.ToString(), Color.GreenYellow, sampleFont);
                }

                if (tower_on == "rotatep")
                {
                    towerdegx = (camPosition.Z - pickedPosition.Z) + towerRotatex;
                    if (towerdegx > 90)
                        towerdegx = 90;
                    if (towerdegx < -90)
                        towerdegx = -90;

                    towerTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(towerdegx)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(towerRotate));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "pitch: " + towerdegx.ToString(), Color.GreenYellow, sampleFont);
                }
                if (tower_on == "rotate")
                {
                    towerdeg = (-camPosition.X + pickedPosition.X) * 2 + towerRotate;
                    if (towerdeg > 180)
                        towerdeg = 180;
                    if (towerdeg < -180)
                        towerdeg = -180;
                    towerTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(towerRotatex)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(towerdeg));
                    UI2DRenderer.WriteText(new Vector2(20, 80), "round: " + towerdeg.ToString(), Color.GreenYellow, sampleFont);
                }
            }
            */

            scene.Draw(elapsedTime, false);
        }

        private void addLaser(Vector3 head, Vector3 tail)
        {
            List<bool> tested = new List<bool>();
            Vector3 p0, p1, p2, ia, ib;
            bool hitted = false;
            int bounce = 2;

            for (int i = 0; i < panelsnum; i++)
            {
                tested.Add(false);
            }

            Material laserMaterial = new Material();
            laserMaterial.Diffuse = new Vector4(0.5f, 0.5f, 0.5f, 1);
            laserMaterial.Specular = Color.White.ToVector4();
            laserMaterial.SpecularPower = 10;

            for (int i = 0; i < lasernum; i++)
            {
                toolbarMarkerNode1.RemoveChild(laserNode[i].Parent);
            }
            lasernum = 0;

            while (bounce-- > 0)
            {

                hitted = false;

                for (int i = 0; i < panelsnum; i++)
                {
                    if (hitted)
                        break;
                    if (tested[i])
                        continue;

                    p0 = Vector3.Transform(panels[i * 4], groundMarkerNode.WorldTransformation);
                    p1 = Vector3.Transform(panels[i * 4 + 1], groundMarkerNode.WorldTransformation);
                    p2 = Vector3.Transform(panels[i * 4 + 2], groundMarkerNode.WorldTransformation);
                    ia = tail; // Vector3.Transform(tail, groundMarkerNode.WorldTransformation);
                    ib = head; // Vector3.Transform(head, groundMarkerNode.WorldTransformation);

                    Matrix coef = new Matrix(ia.X - ib.X, ia.Y - ib.Y, ia.Z - ib.Z, 0, p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z, 0, p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z, 0, 0, 0, 0, 1);
                    if (coef.Determinant() == 0)
                        continue;
                    Vector3 v = ia - p0;
                    Vector3 result = Vector3.Transform(v, Matrix.Invert(coef));
                    resultlabel = result.ToString();
                    Matrix tmp = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
                    Vector3 vv = new Vector3(1, 1, 1);
                    vv = Vector3.Transform(vv, tmp);
                    //resultlabel = vv.ToString();
                    if (result.X > 1 && result.Y <= 2 && result.Y >= 0 && result.Z <= 2 && result.Z >= 0)
                    {
                        laserNode[lasernum] = new GeometryNode("laser");
                        laserNode[lasernum].Model = new Cylinder(2.5f, 2.5f, ((float)result.X - 1) * (head - tail).Length(), 20);

                        laserNode[lasernum].Material = laserMaterial;

                        laserTransNode[lasernum] = new TransformNode();
                        laserTransNode[lasernum].Translation = new Vector3(0, 75 + (((float)result.X - 1) * (head - tail).Length()) / 2, 0);

                        toolbarMarkerNode1.AddChild(laserTransNode[lasernum]);
                        laserTransNode[lasernum].AddChild(laserNode[lasernum]);

                        lasernum++;

                        tail = head + ((float)result.X - 1) * (head - tail);
                        Vector3 inci = new Vector3(head.X - tail.X, head.Y - tail.Y, head.Z - tail.Z);
                        inci.Normalize();
                        Vector3 pnormal = Vector3.Transform(panels[i * 4 + 3], groundMarkerNode.WorldTransformation);
                        Vector3 nt;
                        if (Vector3.Dot(inci, pnormal) < 0)
                            nt = Vector3.Multiply(pnormal, -2 * Vector3.Dot(inci, pnormal));
                        else
                            nt = Vector3.Multiply(pnormal, -2 * Vector3.Dot(inci, pnormal));
                        inci = inci + nt;
                        head = tail + inci;
                        hitted = true;

                        tested[i] = true;

                    }
                }

                if (hitted == false)
                {
                    laserNode[lasernum] = new GeometryNode("laser");
                    laserNode[lasernum].Model = new Cylinder(2.5f, 2.5f, 500, 20);

                    laserNode[lasernum].Material = laserMaterial;

                    laserTransNode[lasernum] = new TransformNode();

                    Vector3 gtail = Vector3.Transform(tail, Matrix.Invert(groundMarkerNode.WorldTransformation));
                    Vector3 ghead = Vector3.Transform(head, Matrix.Invert(groundMarkerNode.WorldTransformation));
                    Vector3 gtrans = (ghead - gtail);
                    gtrans.Normalize();
                    gtrans = Vector3.Multiply(gtrans, 500 / 2);
                    gtrans = gtail + gtrans;

                    laserTransNode[lasernum].Translation = gtrans;

                    toolbarMarkerNode1.AddChild(laserTransNode[lasernum]);
                    laserTransNode[lasernum].AddChild(laserNode[lasernum]);

                    lasernum++;
                    break;
                }



            }

        }

        /*
        private void MouseClickHandler(int button, Point mouseLocation)
        {
            
            //if (box_is_on || cylinder_is_on || craft_is_on || tower_is_on)
            //    return;

            //if (mouseLocation.X > 550 && mouseLocation.X < 750 && mouseLocation.Y > 250 && mouseLocation.Y < 400 && pickedNode != null)
            //    return;

            if (button == MouseInput.LeftButton)
            {
                // 0 means on the near clipping plane, and 1 means on the far clipping plane
                Vector3 nearSource = new Vector3(mouseLocation.X, mouseLocation.Y, 0);
                Vector3 farSource = new Vector3(mouseLocation.X, mouseLocation.Y, 1);

                // Now convert the near and far source to actual near and far 3D points based on our eye location
                // and view frustum
                Vector3 nearPoint = State.Device.Viewport.Unproject(nearSource,
                    State.ProjectionMatrix, State.ViewMatrix * groundMarkerNode.WorldTransformation, Matrix.Identity);
                Vector3 farPoint = State.Device.Viewport.Unproject(farSource,
                    State.ProjectionMatrix, State.ViewMatrix * groundMarkerNode.WorldTransformation, Matrix.Identity);

                // Have the physics engine intersect the pick ray defined by the nearPoint and farPoint with
                // the physics objects in the scene (which we have set up to approximate the model geometry).
#if WINDOWS
                List<PickedObject> pickedObjects = ((NewtonPhysics)scene.PhysicsEngine).PickRayCast(
                    nearPoint, farPoint);
#else
                List<PickedObject> pickedObjects = ((MataliPhysics)scene.PhysicsEngine).PickRayCast(
                    nearPoint, farPoint);
#endif

                if (pickedObjects.Count > 0)
                {
                    pickedObjects.Sort();

                    if (pickedNode != null)
                    {
                        if (pickedNode.Name == "Box")
                        {
                            boxframe.Visible = false;
                            boxNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "cylinder")
                        {
                            cylinderframe.Visible = false;
                            cylinderNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "aircraft")
                        {
                            craftframe.Visible = false;
                            craftNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "tower")
                        {
                            towerframe.Visible = false;
                            towerNode.Model.ShowBoundingBox = false;
                        }
                        pickedNode = null;
                        label = "Nothing is selected";
                    }
                    else
                        pickedNode = (GeometryNode)pickedObjects[0].PickedPhysicsObject.Container;


                    if (pickedNode != null)
                    {
                        label = pickedNode.Name + " is picked";
                        if (pickedNode.Name == "Box")
                        {
                            boxframe.Visible = true;
                            //boxIsPicked = true;
                            boxNode.Model.ShowBoundingBox = true;
                        }
                        if (pickedNode.Name == "cylinder")
                        {
                            cylinderframe.Visible = true;
                            //boxIsPicked = true;
                            cylinderNode.Model.ShowBoundingBox = true;
                        }
                        if (pickedNode.Name == "aircraft")
                        {
                            craftframe.Visible = true;
                            //boxIsPicked = true;
                            craftNode.Model.ShowBoundingBox = true;
                        }
                        if (pickedNode.Name == "tower")
                        {
                            towerframe.Visible = true;
                            //boxIsPicked = true;
                            towerNode.Model.ShowBoundingBox = true;
                        }
                    }

                }
                else
                {
                    if (pickedNode != null)
                    {
                        if (pickedNode.Name == "Box")
                        {
                            boxframe.Visible = false;
                            boxNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "cylinder")
                        {
                            cylinderframe.Visible = false;
                            cylinderNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "aircraft")
                        {
                            craftframe.Visible = false;
                            craftNode.Model.ShowBoundingBox = false;
                        }
                        if (pickedNode.Name == "tower")
                        {
                            towerframe.Visible = false;
                            towerNode.Model.ShowBoundingBox = false;
                        }
                        pickedNode = null;
                    }

                    label = "Nothing is selected";

                }
            }
        }
        */


        private void CreateTags(ContentManager content)
        {
            G2DPanel cameraframe = new G2DPanel();
            cameraframe.Bounds = new Rectangle(550, 25, 200, 200);
            cameraframe.Border = GoblinEnums.BorderFactory.EtchedBorder;
            cameraframe.Texture = content.Load<Texture2D>("camera");
            cameraframe.Transparency = 0.5f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)
            cameraframe.Visible = true;

            G2DButton reset = new G2DButton("reset");
            reset.TextFont = uiFont;
            reset.BorderColor = Color.Green;
            reset.Texture = content.Load<Texture2D>("reset");
            reset.Bounds = new Rectangle(100, 10, 75, 75);
            reset.TextTransparency = 0.5f;
            reset.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            G2DButton transfer = new G2DButton("transfer");
            transfer.TextFont = uiFont;
            transfer.BorderColor = Color.Green;
            transfer.Texture = content.Load<Texture2D>("transfer");
            transfer.Bounds = new Rectangle(10, 10, 75, 75);
            transfer.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            cameraframe.AddChild(transfer);
            cameraframe.AddChild(reset);
            scene.UIRenderer.Add2DComponent(cameraframe);

        }


        private void HandleActionPerformed(object source)
        {
            G2DComponent comp = (G2DComponent)source;

            switch (comp.Text)
            {
                case "reset":
                    ballNode.Physics.InitialLinearVelocity = new Vector3(0, 0, 0);

                    scene.PhysicsEngine.RestartsSimulation();
                    ((MataliPhysics)scene.PhysicsEngine).SetTransform(ballNode.Physics, Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0));
                    ballTransNode.WorldTransformation = Matrix.CreateTranslation(-3 * markerSize, -3 * markerSize + 75, 0);

                    //toolbarMarkerNode1.Update(1 / 30f);

                    //scene.PhysicsEngine.Update(1 / 30f);

                    ball_on = false;
                    break;

                case "Rotation":
                    transMode = "ROTATION";

                    //add code here to change menu items and selector cue

                    Notifier.AddMessage("Rotation Mode Activated");
                    break;

                case "Scaling":
                    transMode = "SCALING";

                    //add code here to change menu items and selector cue

                    Notifier.AddMessage("Scaling Mode Activated");
                    break;
            }
        }

        private void ToolBar1CollideWithObject(MataliPhysicsObject baseObject, MataliPhysicsObject collidingObject)
        {
            string materialName = ((IPhysicsObject)collidingObject.UserTagObj).MaterialName;
            if (materialName == "level1")
            {
                //notify user
                Notifier.AddMessage("Selected the Red Box");

                //record initial position
                initialSelectedPosition = levelNode.WorldTransformation.Translation;
                selectedObj = "level1";
            }
        }

        /*
        private void HandleActionPerformed(object source)
        {
            G2DComponent comp = (G2DComponent)source;

            if (box_is_on || cylinder_is_on || craft_is_on || tower_is_on)
                return;

            if (comp is G2DButton)
            {
                if (comp.Text == "reset")
                {
                    if (isTransfer)
                        return;

                    towerRotate = 90;
                    towerRotatex = 90;
                    towerScale = towerScale0;
                    towerTranslation = towerTranslation0;
                    craftRotate = 90;
                    craftRotatex = 90;
                    craftScale = new Vector3(craftScale0, craftScale0, craftScale0);
                    craftTranslation = craftTranslation0;
                    cylinderRotatex = 90;
                    cylinderRotate = 0;
                    cylinderTranslation = cylinderTranslation0;
                    cylinderScale = new Vector3(1.0f, 1.0f, 1.0f);
                    boxScale = new Vector3(1.0f, 1.0f, 1.0f);
                    boxTranslation = boxTranslation0;
                    boxRotatex = boxRotate = 0;

                    boxTransNode.Translation = boxTranslation;
                    boxTransNode.Scale = boxScale;
                    boxTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(boxRotatex)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.ToRadians(boxRotate));
                    cylinderTransNode.Translation = cylinderTranslation;
                    cylinderTransNode.Scale = cylinderScale;
                    cylinderTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(cylinderRotatex)) * Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(cylinderRotate));
                    craftTransNode.Translation = craftTranslation;
                    craftTransNode.Scale = craftScale;
                    craftTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(craftRotatex)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(craftRotate));
                    towerTransNode.Translation = towerTranslation;
                    towerTransNode.Scale = towerScale;
                    towerTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(towerRotatex)) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(towerRotate));
                }
                if (comp.Text == "transfer")
                {
                    if (!isTransfer && groundMarkerNode.MarkerFound)
                    {
                        groundMarkerNode.RemoveChild(cylinderNode.Parent);
                        scene.RootNode.AddChild(cylinderNode.Parent);
                        cylinderTransNode.WorldTransformation = cylinderTransNode.ComposedTransform * groundMarkerNode.WorldTransformation;

                        groundMarkerNode.RemoveChild(boxNode.Parent);
                        scene.RootNode.AddChild(boxNode.Parent);
                        boxTransNode.WorldTransformation = boxTransNode.ComposedTransform * groundMarkerNode.WorldTransformation;
                        groundMarkerNode.RemoveChild(craftNode.Parent);
                        scene.RootNode.AddChild(craftNode.Parent);
                        craftTransNode.WorldTransformation = craftTransNode.ComposedTransform * groundMarkerNode.WorldTransformation;
                        groundMarkerNode.RemoveChild(towerNode.Parent);
                        scene.RootNode.AddChild(towerNode.Parent);
                        towerTransNode.WorldTransformation = towerTransNode.ComposedTransform * groundMarkerNode.WorldTransformation;

                        lastMarkerWorld = groundMarkerNode.WorldTransformation;
                        isTransfer = true;
                    }
                    else
                        if (groundMarkerNode.MarkerFound)
                        {
                            Quaternion rot;

                            scene.RootNode.RemoveChild(cylinderNode.Parent);
                            groundMarkerNode.AddChild(cylinderNode.Parent);
                            cylinderTransNode.WorldTransformation = cylinderTransNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            cylinderTransNode.WorldTransformation.Decompose(out cylinderScale, out rot , out cylinderTranslation);
                            cylinderTransNode.Translation = cylinderTranslation;
                            cylinderTransNode.Scale = cylinderScale;
                            cylinderTransNode.Rotation = rot;
                            //cylinderTransNode.WorldTransformation = cylinderTransNode.ComposedTransform * Matrix.Invert(lastMarkerWorld);

                            scene.RootNode.RemoveChild(boxNode.Parent);
                            groundMarkerNode.AddChild(boxNode.Parent);
                            boxTransNode.WorldTransformation = boxTransNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            boxTransNode.WorldTransformation.Decompose(out boxScale, out rot, out boxTranslation);
                            boxTransNode.Rotation = rot;
                            boxTransNode.Translation = boxTranslation;
                            boxTransNode.Scale = boxScale;

                            scene.RootNode.RemoveChild(craftNode.Parent);
                            groundMarkerNode.AddChild(craftNode.Parent);
                            craftTransNode.WorldTransformation = craftTransNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            craftTransNode.WorldTransformation.Decompose(out craftScale, out rot, out craftTranslation);
                            craftTransNode.Rotation = rot;
                            craftTransNode.Translation = craftTranslation;
                            craftTransNode.Scale = craftScale;

                            scene.RootNode.RemoveChild(towerNode.Parent);
                            groundMarkerNode.AddChild(towerNode.Parent);
                            towerTransNode.WorldTransformation = towerTransNode.WorldTransformation * Matrix.Invert(groundMarkerNode.WorldTransformation);
                            towerTransNode.WorldTransformation.Decompose(out towerScale, out rot, out towerTranslation);
                            towerTransNode.Rotation = rot;
                            towerTransNode.Translation = towerTranslation;
                            towerTransNode.Scale = towerScale;

                            isTransfer = false;
                        }
                }
            }

        }
        */

        /*
        private void CreateTags1(ContentManager content)
        {
            cylinderframe = new G2DPanel();
            cylinderframe.Bounds = new Rectangle(550, 250, 200, 200);
            cylinderframe.Border = GoblinEnums.BorderFactory.EtchedBorder;
            cylinderframe.Texture = content.Load<Texture2D>("object");
            cylinderframe.Transparency = 0.5f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)
            cylinderframe.Visible = false;

            G2DButton rotatep = new G2DButton("rotatep");
            rotatep.TextFont = uiFont;
            rotatep.BorderColor = Color.Green;
            rotatep.Texture = content.Load<Texture2D>("rotate");
            rotatep.Bounds = new Rectangle(100, 90, 75, 75);
            rotatep.TextTransparency = 0.5f;
            rotatep.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed1);

            G2DButton rotate = new G2DButton("rotate");
            rotate.TextFont = uiFont;
            rotate.BorderColor = Color.Green;
            rotate.Texture = content.Load<Texture2D>("rotation");
            rotate.Bounds = new Rectangle(10, 90, 75, 75);
            rotate.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed1);

            G2DButton scale = new G2DButton("scale");
            scale.TextFont = uiFont;
            scale.BorderColor = Color.Green;
            scale.Texture = content.Load<Texture2D>("S");
            scale.Bounds = new Rectangle(10, 10, 75, 75);
            scale.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed1);

            G2DButton trans = new G2DButton("translate");
            trans.TextFont = uiFont;
            trans.BorderColor = Color.Green;
            trans.Texture = content.Load<Texture2D>("t");
            trans.Bounds = new Rectangle(100, 10, 75, 75);
            trans.TextTransparency = 0.5f;
            trans.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed1);

            cylinderframe.AddChild(trans);
            cylinderframe.AddChild(rotate);
            cylinderframe.AddChild(rotatep);
            cylinderframe.AddChild(scale);
            scene.UIRenderer.Add2DComponent(cylinderframe);

        }

        private void CreateTags2(ContentManager content)
        {
            boxframe = new G2DPanel();
            boxframe.Bounds = new Rectangle(550, 250, 200, 200);
            boxframe.Border = GoblinEnums.BorderFactory.EtchedBorder;
            boxframe.Texture = content.Load<Texture2D>("object");
            boxframe.Transparency = 0.5f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)
            boxframe.Visible = false;

            G2DButton rotatep = new G2DButton("rotatep");
            rotatep.TextFont = uiFont;
            rotatep.BorderColor = Color.Green;
            rotatep.Texture = content.Load<Texture2D>("rotate");
            rotatep.Bounds = new Rectangle(100, 90, 75, 75);
            rotatep.TextTransparency = 0.5f;
            rotatep.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed2);

            G2DButton rotate = new G2DButton("rotate");
            rotate.TextFont = uiFont;
            rotate.BorderColor = Color.Green;
            rotate.Texture = content.Load<Texture2D>("rotation");
            rotate.Bounds = new Rectangle(10, 90, 75, 75);
            rotate.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed2);

            G2DButton scale = new G2DButton("scale");
            scale.TextFont = uiFont;
            scale.BorderColor = Color.Green;
            scale.Texture = content.Load<Texture2D>("S");
            scale.Bounds = new Rectangle(10, 10, 75, 75);
            scale.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed2);

            G2DButton trans = new G2DButton("translate");
            trans.TextFont = uiFont;
            trans.BorderColor = Color.Green;
            trans.Texture = content.Load<Texture2D>("t");
            trans.Bounds = new Rectangle(100, 10, 75, 75);
            trans.TextTransparency = 0.5f;
            trans.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed2);

            boxframe.AddChild(trans);
            boxframe.AddChild(rotate);
            boxframe.AddChild(rotatep);
            boxframe.AddChild(scale);
            scene.UIRenderer.Add2DComponent(boxframe);

        }

        private void CreateTags3(ContentManager content)
        {
            craftframe = new G2DPanel();
            craftframe.Bounds = new Rectangle(550, 250, 200, 200);
            craftframe.Border = GoblinEnums.BorderFactory.EtchedBorder;
            craftframe.Texture = content.Load<Texture2D>("object");
            craftframe.Transparency = 0.5f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)
            craftframe.Visible = false;

            G2DButton rotatep = new G2DButton("rotatep");
            rotatep.TextFont = uiFont;
            rotatep.BorderColor = Color.Green;
            rotatep.Texture = content.Load<Texture2D>("rotate");
            rotatep.Bounds = new Rectangle(100, 90, 75, 75);
            rotatep.TextTransparency = 0.5f;
            rotatep.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed3);

            G2DButton rotate = new G2DButton("rotate");
            rotate.TextFont = uiFont;
            rotate.BorderColor = Color.Green;
            rotate.Texture = content.Load<Texture2D>("rotation");
            rotate.Bounds = new Rectangle(10, 90, 75, 75);
            rotate.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed3);

            G2DButton scale = new G2DButton("scale");
            scale.TextFont = uiFont;
            scale.BorderColor = Color.Green;
            scale.Texture = content.Load<Texture2D>("S");
            scale.Bounds = new Rectangle(10, 10, 75, 75);
            scale.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed3);

            G2DButton trans = new G2DButton("translate");
            trans.TextFont = uiFont;
            trans.BorderColor = Color.Green;
            trans.Texture = content.Load<Texture2D>("t");
            trans.Bounds = new Rectangle(100, 10, 75, 75);
            trans.TextTransparency = 0.5f;
            trans.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed3);

            craftframe.AddChild(trans);
            craftframe.AddChild(rotate);
            craftframe.AddChild(rotatep);
            craftframe.AddChild(scale);
            scene.UIRenderer.Add2DComponent(craftframe);

        }

        private void CreateTags4(ContentManager content)
        {
            towerframe = new G2DPanel();
            towerframe.Bounds = new Rectangle(550, 250, 200, 200);
            towerframe.Border = GoblinEnums.BorderFactory.EtchedBorder;
            towerframe.Texture = content.Load<Texture2D>("object");
            towerframe.Transparency = 0.5f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)
            towerframe.Visible = false;

            G2DButton rotatep = new G2DButton("rotatep");
            rotatep.TextFont = uiFont;
            rotatep.BorderColor = Color.Green;
            rotatep.Texture = content.Load<Texture2D>("rotate");
            rotatep.Bounds = new Rectangle(100, 90, 75, 75);
            rotatep.TextTransparency = 0.5f;
            rotatep.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed4);

            G2DButton rotate = new G2DButton("rotate");
            rotate.TextFont = uiFont;
            rotate.BorderColor = Color.Green;
            rotate.Texture = content.Load<Texture2D>("rotation");
            rotate.Bounds = new Rectangle(10, 90, 75, 75);
            rotate.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed4);

            G2DButton scale = new G2DButton("scale");
            scale.TextFont = uiFont;
            scale.BorderColor = Color.Green;
            scale.Texture = content.Load<Texture2D>("S");
            scale.Bounds = new Rectangle(10, 10, 75, 75);
            scale.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed4);

            G2DButton trans = new G2DButton("translate");
            trans.TextFont = uiFont;
            trans.BorderColor = Color.Green;
            trans.Texture = content.Load<Texture2D>("t");
            trans.Bounds = new Rectangle(100, 10, 75, 75);
            trans.TextTransparency = 0.5f;
            trans.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed4);

            towerframe.AddChild(trans);
            towerframe.AddChild(rotate);
            towerframe.AddChild(rotatep);
            towerframe.AddChild(scale);
            scene.UIRenderer.Add2DComponent(towerframe);

        }

        private void HandleActionPerformed1(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DButton)
            {
                if (comp.Text == "rotatep")
                {
                    if (cylinder_is_on == false)
                    {
                        cylinder_is_on = true;
                        cylinder_on = "rotatep";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (cylinder_on == "rotatep")
                        {
                            cylinder_is_on = false;
                            cylinderRotatex = cylinderdegx;
                        }
                }
                if (comp.Text == "rotate")
                {
                    if (cylinder_is_on == false)
                    {
                        cylinder_is_on = true;
                        cylinder_on = "rotate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (cylinder_on == "rotate")
                        {
                            cylinder_is_on = false;
                            cylinderRotate = cylinderdeg;
                        }
                }
                if (comp.Text == "translate")
                {
                    if (cylinder_is_on == false)
                    {
                        cylinder_is_on = true;
                        cylinder_on = "translate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (cylinder_on == "translate")
                        {
                            cylinder_is_on = false;
                            cylinderTranslation = cylinderTransNode.Translation;
                        }
                }
                if (comp.Text == "scale")
                {
                    if (cylinder_is_on == false)
                    {
                        cylinder_is_on = true;
                        cylinder_on = "scale";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (cylinder_on == "scale")
                        {
                            cylinderScale = cylinderTransNode.Scale;
                            cylinder_is_on = false;
                        }
                }
            }

        }

        private void HandleActionPerformed2(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DButton)
            {
                if (comp.Text == "rotatep")
                {
                    if (box_is_on == false)
                    {
                        box_is_on = true;
                        box_on = "rotatep";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (box_on == "rotatep")
                        {
                            box_is_on = false;
                            boxRotatex = boxdegx;
                        }
                }
                if (comp.Text == "rotate")
                {
                    if (box_is_on == false)
                    {
                        box_is_on = true;
                        box_on = "rotate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (box_on == "rotate")
                        {
                            box_is_on = false;
                            boxRotate = boxdeg;
                        }
                }
                if (comp.Text == "translate")
                {
                    if (box_is_on == false)
                    {
                        box_is_on = true;
                        box_on = "translate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (box_on == "translate")
                        {
                            box_is_on = false;
                            boxTranslation = boxTransNode.Translation;
                        }
                }
                if (comp.Text == "scale")
                {
                    if (box_is_on == false)
                    {
                        box_is_on = true;
                        box_on = "scale";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (box_on == "scale")
                        {
                            boxScale = boxTransNode.Scale;
                            box_is_on = false;
                        }
                }
            }

        }

        private void HandleActionPerformed3(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DButton)
            {
                if (comp.Text == "rotatep")
                {
                    if (craft_is_on == false)
                    {
                        craft_is_on = true;
                        craft_on = "rotatep";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (craft_on == "rotatep")
                        {
                            craft_is_on = false;
                            craftRotatex = craftdegx;
                        }
                }
                if (comp.Text == "rotate")
                {
                    if (craft_is_on == false)
                    {
                        craft_is_on = true;
                        craft_on = "rotate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (craft_on == "rotate")
                        {
                            craft_is_on = false;
                            craftRotate = craftdeg;
                        }
                }
                if (comp.Text == "translate")
                {
                    if (craft_is_on == false)
                    {
                        craft_is_on = true;
                        craft_on = "translate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (craft_on == "translate")
                        {
                            craft_is_on = false;
                            craftTranslation = craftTransNode.Translation;
                        }
                }
                if (comp.Text == "scale")
                {
                    if (craft_is_on == false)
                    {
                        craft_is_on = true;
                        craft_on = "scale";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (craft_on == "scale")
                        {
                            craftScale = craftTransNode.Scale;
                            craft_is_on = false;
                        }
                }
            }

        }

        private void HandleActionPerformed4(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DButton)
            {
                if (comp.Text == "rotatep")
                {
                    if (tower_is_on == false)
                    {
                        tower_is_on = true;
                        tower_on = "rotatep";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (tower_on == "rotatep")
                        {
                            tower_is_on = false;
                            towerRotatex = towerdegx;
                        }
                }
                if (comp.Text == "rotate")
                {
                    if (tower_is_on == false)
                    {
                        tower_is_on = true;
                        tower_on = "rotate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (tower_on == "rotate")
                        {
                            tower_is_on = false;
                            towerRotate = towerdeg;
                        }
                }
                if (comp.Text == "translate")
                {
                    if (tower_is_on == false)
                    {
                        tower_is_on = true;
                        tower_on = "translate";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (tower_on == "translate")
                        {
                            tower_is_on = false;
                            towerTranslation = towerTransNode.Translation;
                        }
                }
                if (comp.Text == "scale")
                {
                    if (tower_is_on == false)
                    {
                        tower_is_on = true;
                        tower_on = "scale";
                        pickedPosition = Vector3.Transform(new Vector3(0, 0, markerSize / 2), Matrix.Invert(groundMarkerNode.WorldTransformation));
                    }
                    else
                        if (tower_on == "scale")
                        {
                            towerScale = towerTransNode.Scale;
                            tower_is_on = false;
                        }
                }
            }

        }
        */


    }
}
