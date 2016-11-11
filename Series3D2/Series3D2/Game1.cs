using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Series3D2
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        GraphicsDevice device;//
        Effect effect;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        Texture2D sceneryTexture;//
        VertexPositionTexture[] vertices;
        int[,] floorPlan;//
        VertexBuffer cityVertexBuffer;
        int[] buildingHeights = new int[] { 0, 2, 2, 6, 5, 4 };//
        Model xwingModel;//
        Vector3 lightDirection = new Vector3(3, -2, 5);//
        Vector3 xwingPosition = new Vector3(8, 1, -3);//
        Quaternion xwingRotation = Quaternion.Identity;
        float gameSpeed = 1.0f;//
        enum CollisionType { None, Building, Boundary, Target }//
        BoundingBox[] buildingBoundingBoxes;
        BoundingBox completeCityBox;
        const int maxTargets = 50;//
        Model targetModel;
        List<BoundingSphere> targetList = new List<BoundingSphere>();
        struct Bullet//
        {
            public Vector3 position;
            public Quaternion rotation;
        }
        Texture2D bulletTexture;
        List<Bullet> bulletList = new List<Bullet>();
        double lastBulletTime = 0;
        Vector3 cameraPosition;
        Vector3 cameraUpDirection;
        Texture2D[] skyboxTextures;//
        Model skyboxModel;
        Quaternion cameraRotation = Quaternion.Identity;//
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = 500;//
            graphics.PreferredBackBufferHeight = 500;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Window.Title = "Riemer's XNA Tutorials -- 3D Series 2";
            LoadFloorPlan();//
            lightDirection.Normalize();//
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            // TODO: use this.Content to load your game content here
            device = graphics.GraphicsDevice;//
            effect = Content.Load<Effect>("effects"); 
            skyboxModel = LoadModel("skybox2", out skyboxTextures);//
            LoadFloorPlan();
            SetUpCamera();
            sceneryTexture = Content.Load<Texture2D>("texturemap");//
            SetUpVertices();
            xwingModel = LoadModel("xwing");//
            SetUpBoundingBoxes();//
            targetModel = LoadModel("target");//
            AddTargets();
            bulletTexture = Content.Load<Texture2D>("bullet");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            // TODO: Add your update logic here
            ProcessKeyboard(gameTime);
            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 500.0f * gameSpeed;
            MoveForward(ref xwingPosition, xwingRotation, moveSpeed);
            BoundingSphere xwingSpere = new BoundingSphere(xwingPosition, 0.04f);//
            if (CheckCollision(xwingSpere) != CollisionType.None)
            {
                xwingPosition = new Vector3(8, 1, -3);
                xwingRotation = Quaternion.Identity;
                gameSpeed /= 1.1f;
            }
            UpdateCamera();
            UpdateBulletPositions(moveSpeed);//
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);
            // TODO: Add your drawing code here
            DrawSkybox();//
            DrawCity();//
            DrawModel();//
            DrawTargets();//
            DrawBullets();//
            base.Draw(gameTime);
        }
        private void SetUpCamera()//
        {
            viewMatrix = Matrix.CreateLookAt(new Vector3(20, 13, -5), new Vector3(8, 0, -7), new Vector3(0, 1, 0)); 
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);
        }
        private void SetUpVertices()//
        {
            int differentBuildings = buildingHeights.Length - 1;
            float imagesInTexture = 1 + differentBuildings * 2;
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            List<VertexPositionNormalTexture> verticesList = new List<VertexPositionNormalTexture>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int currentbuilding = floorPlan[x, z];
                    //floor or ceiling
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2(currentbuilding * 2 / imagesInTexture, 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 0)));
                    verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 1, 0), new Vector2((currentbuilding * 2 + 1) / imagesInTexture, 1)));
                    if (currentbuilding != 0)
                    {
                        //front wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(0, 0, -1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        //back wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(0, 0, 1), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        //left wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z - 1), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, buildingHeights[currentbuilding], -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x, 0, -z), new Vector3(-1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        //right wall
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z - 1), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2 - 1) / imagesInTexture, 0)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, 0, -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 1)));
                        verticesList.Add(new VertexPositionNormalTexture(new Vector3(x + 1, buildingHeights[currentbuilding], -z), new Vector3(1, 0, 0), new Vector2((currentbuilding * 2) / imagesInTexture, 0)));
                    }
                }
            }
            cityVertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, verticesList.Count, BufferUsage.WriteOnly);
            cityVertexBuffer.SetData<VertexPositionNormalTexture>(verticesList.ToArray());
        }
        private void LoadFloorPlan()//
        {
            floorPlan = new int[,]
            {/*
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,1,1,0,0,0,1,1,0,0,1,0,1},
                {1,0,0,1,1,0,0,0,1,0,0,0,1,0,1},
                {1,0,0,0,1,1,0,1,1,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,1,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,1,1,0,0,0,1,0,0,0,0,0,0,1},
                {1,0,1,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,1,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,1,0,0,0,1,0,0,0,0,1},
                {1,0,1,0,0,0,0,0,0,1,0,0,0,0,1},
                {1,0,1,1,0,0,0,0,1,1,0,0,0,1,1},
                {1,0,0,0,0,0,0,0,1,1,0,0,0,1,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                */
                
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},

                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
            };
            Random random = new Random();//
            int differentBuildings = buildingHeights.Length - 1;
            for (int x = 0; x < floorPlan.GetLength(0); x++)
                for (int y = 0; y < floorPlan.GetLength(1); y++)
                    if (floorPlan[x, y] == 1)
                        floorPlan[x, y] = random.Next(differentBuildings) + 1;
        }
        private void DrawCity()//
        {
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xTexture"].SetValue(sceneryTexture);
            effect.Parameters["xEnableLighting"].SetValue(true);//
            effect.Parameters["xLightDirection"].SetValue(lightDirection);
            effect.Parameters["xAmbient"].SetValue(0.5f);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.SetVertexBuffer(cityVertexBuffer);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, cityVertexBuffer.VertexCount / 3);
            }
        }
        private Model LoadModel(string assetName)
        {
            Model newModel = Content.Load<Model>(assetName); 
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();
            return newModel;
        }
        private void DrawModel()//
        {
            Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateFromQuaternion(xwingRotation) * Matrix.CreateTranslation(xwingPosition);//

            //Matrix worldMatrix = Matrix.CreateScale(0.0005f, 0.0005f, 0.0005f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateTranslation(new Vector3(19, 12, -5));
            Matrix[] xwingTransforms = new Matrix[xwingModel.Bones.Count];
            xwingModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            foreach (ModelMesh mesh in xwingModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);//
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                }
                mesh.Draw();
            }
        }
        private void UpdateCamera()//
        {
            cameraRotation = Quaternion.Lerp(cameraRotation, xwingRotation, 0.05f);

            Vector3 campos = new Vector3(0, 0.1f, 0.6f);
            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            campos += xwingPosition;
            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            //Vector3 campos = new Vector3(0, 0.1f, 0.6f);
            //campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(xwingRotation));
            //campos += xwingPosition;
            //Vector3 camup = new Vector3(0, 1, 0);
            //camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(xwingRotation));

            viewMatrix = Matrix.CreateLookAt(campos, xwingPosition, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.2f, 500.0f);
            cameraPosition = campos;
            cameraUpDirection = camup;
        }
        private void ProcessKeyboard(GameTime gameTime)//
        {
            float gameSpeed = 1.0f;
            float leftRightRot = 0;
            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turningSpeed *= 1.6f * gameSpeed;
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Right))
                leftRightRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Left))
                leftRightRot -= turningSpeed;
            float upDownRot = 0;
            if (keys.IsKeyDown(Keys.Down))
                upDownRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Up))
                upDownRot -= turningSpeed;
            Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -0.6f), leftRightRot) * Quaternion.CreateFromAxisAngle(new Vector3(0.6f, 0, 0), upDownRot);
            xwingRotation *= additionalRot;
            if (keys.IsKeyDown(Keys.Space))//
            {
                double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
                if (currentTime - lastBulletTime > 100)
                {
                    Bullet newBullet = new Bullet();
                    newBullet.position = xwingPosition;
                    newBullet.rotation = xwingRotation;
                    bulletList.Add(newBullet);
                    lastBulletTime = currentTime;
                }
            }
        }
        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed)//
        {
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), rotationQuat);
            position += addVector * speed;
        }
        private void SetUpBoundingBoxes()//
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            List<BoundingBox> bbList = new List<BoundingBox>();
            for (int x = 0; x < cityWidth; x++)
            {
                for (int z = 0; z < cityLength; z++)
                {
                    int buildingType = floorPlan[x, z];
                    if (buildingType != 0)
                    {
                        int buildingHeight = buildingHeights[buildingType];
                        Vector3[] buildingPoints = new Vector3[2];
                        buildingPoints[0] = new Vector3(x, 0, -z);
                        buildingPoints[1] = new Vector3(x + 1, buildingHeight, -z - 1);
                        BoundingBox buildingBox = BoundingBox.CreateFromPoints(buildingPoints);
                        bbList.Add(buildingBox);
                    }
                }
            }
            buildingBoundingBoxes = bbList.ToArray();
            Vector3[] boundaryPoints = new Vector3[2];
            boundaryPoints[0] = new Vector3(0, 0, 0);
            boundaryPoints[1] = new Vector3(cityWidth, 20, -cityLength);
            completeCityBox = BoundingBox.CreateFromPoints(boundaryPoints);
        }
        private CollisionType CheckCollision(BoundingSphere sphere)//
        {
            for (int i = 0; i < buildingBoundingBoxes.Length; i++)
                if (buildingBoundingBoxes[i].Contains(sphere) != ContainmentType.Disjoint)
                    return CollisionType.Building;
            if (completeCityBox.Contains(sphere) != ContainmentType.Contains)
                return CollisionType.Boundary;
            for (int i = 0; i < targetList.Count; i++)//
            {
                if (targetList[i].Contains(sphere) != ContainmentType.Disjoint)
                {
                    targetList.RemoveAt(i);
                    i--;
                    AddTargets();
                    return CollisionType.Target;
                }
            }
            return CollisionType.None;
        }
        private void AddTargets()//
        {
            int cityWidth = floorPlan.GetLength(0);
            int cityLength = floorPlan.GetLength(1);
            Random random = new Random();
            while (targetList.Count < maxTargets)
            {
                int x = random.Next(cityWidth);
                int z = -random.Next(cityLength);
                float y = (float)random.Next(2000) / 1000f + 1;
                float radius = (float)random.Next(1000) / 1000f * 0.2f + 0.01f;
                BoundingSphere newTarget = new BoundingSphere(new Vector3(x, y, z), radius);
                if (CheckCollision(newTarget) == CollisionType.None)
                    targetList.Add(newTarget);
            }
        }
        private void DrawTargets()//
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                Matrix worldMatrix = Matrix.CreateScale(targetList[i].Radius) * Matrix.CreateTranslation(targetList[i].Center);
                Matrix[] targetTransforms = new Matrix[targetModel.Bones.Count];
                targetModel.CopyAbsoluteBoneTransformsTo(targetTransforms);
                foreach (ModelMesh modmesh in targetModel.Meshes)
                {
                    foreach (Effect currentEffect in modmesh.Effects)
                    {
                        currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                        currentEffect.Parameters["xWorld"].SetValue(targetTransforms[modmesh.ParentBone.Index] * worldMatrix);
                        currentEffect.Parameters["xView"].SetValue(viewMatrix);
                        currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                        currentEffect.Parameters["xEnableLighting"].SetValue(true);
                        currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                        currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    }
                    modmesh.Draw();
                }
            }
        }
        private void UpdateBulletPositions(float moveSpeed)//
        {
            for (int i = 0; i < bulletList.Count; i++)
            {
                Bullet currentBullet = bulletList[i];
                MoveForward(ref currentBullet.position, currentBullet.rotation, moveSpeed * 2.0f);
                bulletList[i] = currentBullet;
                BoundingSphere bulletSphere = new BoundingSphere(currentBullet.position, 0.05f);//
                CollisionType colType = CheckCollision(bulletSphere);
                if (colType != CollisionType.None)
                {
                    bulletList.RemoveAt(i);
                    i--;
                    if (colType == CollisionType.Target)
                        gameSpeed *= 1.05f;
                }
            }
        }
        private void DrawBullets()//
        {
            if (bulletList.Count > 0)
            {
                VertexPositionTexture[] bulletVertices = new VertexPositionTexture[bulletList.Count * 6];
                int i = 0;
                foreach (Bullet currentBullet in bulletList)
                {
                    Vector3 center = currentBullet.position;
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 0));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 1));
                    bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                }
                effect.CurrentTechnique = effect.Techniques["PointSprites"];
                effect.Parameters["xWorld"].SetValue(Matrix.Identity);
                effect.Parameters["xProjection"].SetValue(projectionMatrix);
                effect.Parameters["xView"].SetValue(viewMatrix);
                effect.Parameters["xCamPos"].SetValue(cameraPosition);
                effect.Parameters["xTexture"].SetValue(bulletTexture);
                effect.Parameters["xCamUp"].SetValue(cameraUpDirection);
                effect.Parameters["xPointSpriteSize"].SetValue(0.1f);
                device.BlendState = BlendState.Additive;
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, bulletVertices, 0, bulletList.Count * 2);
                }
                device.BlendState = BlendState.Opaque;
            }
        }
        private Model LoadModel(string assetName, out Texture2D[] textures)//
        {

            Model newModel = Content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes.Count];
            int i = 0;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (BasicEffect currentEffect in mesh.Effects)
                    textures[i++] = currentEffect.Texture;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();
            return newModel;
        }
        private void DrawSkybox()//
        {
            SamplerState ss = new SamplerState();
            ss.AddressU = TextureAddressMode.Clamp;
            ss.AddressV = TextureAddressMode.Clamp;
            device.SamplerStates[0] = ss;
            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = false;
            device.DepthStencilState = dss;
            Matrix[] skyboxTransforms = new Matrix[skyboxModel.Bones.Count];
            skyboxModel.CopyAbsoluteBoneTransformsTo(skyboxTransforms);
            int i = 0;
            foreach (ModelMesh mesh in skyboxModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = skyboxTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(xwingPosition);
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(skyboxTextures[i++]);
                }
                mesh.Draw();
            }
            dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            device.DepthStencilState = dss;
        }
    }
}
