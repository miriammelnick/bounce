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

namespace BounceLib
{
    public class Tutorial8_Phone
    {
        SpriteFont sampleFont;
        SpriteFont uiFont;
        Scene scene;
        MarkerNode groundMarkerNode, toolbarMarkerNode1, toolbarMarkerNode2;
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

        GeometryNode levelNode;
        TransformNode levelTransNode;
        TransformNode poleTransNode;
        GeometryNode poleNode;
        TransformNode ballTransNode;
        GeometryNode ballNode;
        TransformNode laserGroup;

        /* Scaling and Rotation Variables */
        string transMode = "ROTATION"; //designates transformation mode, takes on values of "ROTATION" and "SCALING"
        string selectedObj = ""; //designates selected object by name
        Vector3 initialSelectedPosition = new Vector3(-1, -1, -1); //stores initial position of selected object

        bool ball_on = false;
        string resultlabel = "";
        int countdown = 0;
        int laserNum = 0;

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

            CreateTags(content);

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

            CreateModels();

        }

        private void CreateModels()
        {
            ModelLoader loader = new ModelLoader();

            #region load the models for the first level
            levelNode = new GeometryNode("level1");
            levelNode.Model = (Model)loader.Load("", "bouncelevel1panels");
            ((Model)levelNode.Model).UseInternalMaterials = true;
            Vector3 dimension = Vector3Helper.GetDimensions(levelNode.Model.MinimumBoundingBox);
            float scale2 = markerSize / Math.Max(dimension.X, dimension.Z) * 5;
            levelTransNode = new TransformNode()
            {
                Translation = new Vector3(-markerSize, -3 * markerSize, 0),
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
            #endregion

            #region load the cue stick models
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
            ((MataliObject)poleNode.Physics).CollisionStartCallback = ToolBar1CollideWithObject;
            poleNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            //poleNode.Physics.Interactable = true;
            //craftNode.Physics.Pickable = true;
            poleNode.Physics.Collidable = true;
            poleNode.Physics.Mass = 100f;

            //craftNode.Physics.InitialLinearVelocity = Vector3.Zero;
            poleNode.AddToPhysicsEngine = true;

            poleTransNode = new TransformNode();
            poleTransNode.Translation = new Vector3(0, 0, 0);
            poleTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize, 0);
            //poleTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90));

            groundMarkerNode.AddChild(poleTransNode);
            //toolbarMarkerNode1.AddChild(poleTransNode);
            poleTransNode.AddChild(poleNode);
            #endregion

            #region load the ball object
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
            #endregion

            laserGroup = new TransformNode();
            groundMarkerNode.AddChild(laserGroup);
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
            if (materialName == "level1")
            {
                //notify user
                Notifier.AddMessage("Selected the level1 panels!");

                //record initial position
                initialSelectedPosition = levelNode.WorldTransformation.Translation;
                selectedObj = "level1";
            }
        }


        private void createLaser()
        {
            if (laserNum > 5){
                laserGroup.RemoveChildAt(0);                
                
            }

            laserNum++;

            GeometryNode laserNode = new GeometryNode("Ball");
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

            laserNode.Physics.Interactable = true;
            //craftNode.Physics.Pickable = true;
            laserNode.Physics.Collidable = true;
            laserNode.Physics.Mass = 10f;
            //ballNode.Physics.InitialLinearVelocity = Vector3.Zero;

            TransformNode laserTransNode = new TransformNode();
            laserTransNode.Translation = new Vector3(0, 75, 0);
            laserTransNode.Translation = new Vector3(-3 * markerSize, -3 * markerSize + 75, 0);

            laserGroup.AddChild(laserTransNode);
            //toolbarMarkerNode1.AddChild(ballTransNode);
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
                Vector3 shiftVector2 = new Vector3(0, 95, 0);
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
                    linVel *= 2000f;

                    ballNode.Physics.InitialLinearVelocity = linVel;

                    laserGroup.RemoveChildren();
                    laserNum = 0;
                    scene.PhysicsEngine.RestartsSimulation();
                    //toolbarMarkerNode1.Update(1 / 30f);

                    //scene.PhysicsEngine.Update(1 / 30f);

                    ball_on = true;
                    label = "Shoot!";

                }

            }

            if (countdown++ == 10)
            {
                if (toolbarMarkerNode1.MarkerFound && !ball_on)
                    createLaser();
                countdown = 0;
            }

            label = countdown.ToString();

            scene.Draw(elapsedTime, false);
        }

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


    }
}
