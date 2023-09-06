using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;

namespace ray_marching
{
    public partial class Form1 : Form
    {
        private bool up, left, down, right;
        private List<float> radii = new List<float>();
        HitResult[] hitResults;
        float maxspeed = 5f;
        private const int max = 20;
        const float fov = (float)Math.PI / 3f;
        const int screenResolution = 800;
        //private float circleSize = 0;
        private Random r = new Random();
        private Bitmap render;
        Circle player;
        List<Shape> shapes;
        Vector2 velocity = new Vector2();
        private float theta = 0f;
        const float maxDst = 10000;
        const float minDst = 0.001f;
        const int maxSteps = 2000;
        class Ray
        {
            public Vector2 origin;
            public Vector2 direction;
        }
        abstract class Shape
        {
            public Vector2 Position;
            public Vector2 Size;
            public Color Color;
            public abstract float Distance(Vector2 p);
        }
        class Circle : Shape
        {
            public override float Distance(Vector2 p)
            {
                return length(Position - p) - Size.X;
            }
        }
        class StarFive : Shape
        {
            public override float Distance(Vector2 p)
            {
                if (float.IsNaN(p.X))
                {
                    return maxDst;
                }
                Vector2 k1 = new Vector2(0.809016994375f, -0.587785252292f);
                Vector2 k2 = new Vector2(-k1.X, k1.Y);
                p = Position - p;
                p.X = Math.Abs(p.X);
                p -= 2.0f * (float)Math.Max(Vector2.Dot(k1, p), 0.0) * k1;
                p -= 2.0f * (float)Math.Max(Vector2.Dot(k2, p), 0.0) * k2;
                p.X = Math.Abs(p.X);
                p.Y -= Size.X;
                Vector2 ba = Size.Y * new Vector2(-k1.Y, k1.X) - Vector2.UnitY;
                float h = Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0.0f, Size.X);
                return length(p - ba * h) * Math.Sign(p.Y * ba.X - p.X * ba.Y);
            }

        }
        public static float Clamp(float num, float min, float max)
        {
            if (num.CompareTo(min) < 0) return min;
            if (num.CompareTo(max) > 0) return max;
            return num;
        }
        class Box : Shape
        {
            public override float Distance(Vector2 p)
            {
                Vector2 offset = Vector2.Abs(p - Position) - Size;
                float unsignedDst = length(Vector2.Max(offset, Vector2.Zero));
                float dstInsideBox = Math.Max(Vector2.Min(offset, Vector2.Zero).X, Vector2.Min(offset, Vector2.Zero).Y);
                return unsignedDst + dstInsideBox;
            }
        }
        public int formWidth = 800;
        public int formHeight = 400;
        public Form1()
        {
            InitializeComponent();
        }

        float signedDstToCircle(Vector2 p, Vector2 Position, float radius)
        {
            return length(Position - p) - radius;
        }

        public static float length(Vector2 v)
        {
            return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        float signedDstToScene(Vector2 p)
        {
            float dstToScene = maxDst;

            foreach (var item in shapes)
            {
                dstToScene = Math.Min(item.Distance(p), dstToScene);
            }
            return dstToScene;
        }

        float signedDstToScene(Vector2 p, ref Color c)
        {
            float dstToScene = maxDst;
            float dst = maxDst;
            foreach (var item in shapes)
            {
                dst = item.Distance(p);
                if (dst < dstToScene)
                {
                    dstToScene = dst;
                    c = item.Color;
                }
                if (dst < minDst)
                {
                    return dstToScene;
                }
            }

            return dstToScene;
        }
        Graphics g;
        private void Form1_Load(object sender, EventArgs e)
        {
            shapes = new List<Shape>();
            Width = formWidth;
            Height = formHeight;
            render = new Bitmap(screenResolution, formHeight);
            player = new Circle
            {
                Position = new Vector2(Width - 100, Height - 100),
                Size = new Vector2(10, 10),
                Color = Color.Purple,
            };

            DoubleBuffered = true;
            for (int i = 0; i < max; i++)
            {
                if (r.NextDouble() > 0.66)
                {
                    shapes.Add(new Circle
                    {
                        Position = new Vector2(r.Next(0, Width), r.Next(0, Width)),
                        Size = new Vector2(r.Next(5, 40)),
                        Color = Color.Red
                    });
                }
                else if (r.NextDouble() > 0.33)
                {
                    shapes.Add(new Box
                    {
                        Position = new Vector2(r.Next(0, Width), r.Next(0, Width)),
                        Size = new Vector2(r.Next(5, 40), r.Next(5, 40)),
                        Color = Color.Blue
                    });
                }
                else
                {
                    shapes.Add(new StarFive
                    {
                        Position = new Vector2(r.Next(0, Width), r.Next(0, Height)),
                        Size = new Vector2(r.Next(5, 5), r.Next(5, 5)),
                        Color = Color.Purple
                    });
                }
            }
            //shapes.Add(new StarFive
            //{
            //    Position = new Vector2(r.Next(50, 50)),
            //    Size = new Vector2(5,5),
            //    Color = Color.Purple
            //});
            shapes.Add(new Box { Position = new Vector2(-3, ClientSize.Height / 2), Size = new Vector2(6, ClientSize.Height / 2), Color = Color.Blue });
            shapes.Add(new Box { Position = new Vector2(ClientSize.Width + 3, ClientSize.Height / 2), Size = new Vector2(6, ClientSize.Height / 2), Color = Color.Blue });
            shapes.Add(new Box { Position = new Vector2(ClientSize.Width / 2, -3), Size = new Vector2(ClientSize.Width / 2, 6), Color = Color.Blue });
            shapes.Add(new Box { Position = new Vector2(ClientSize.Width / 2, ClientSize.Height + 3), Size = new Vector2(ClientSize.Width / 2, 6), Color = Color.Blue });
            Height = (formHeight * 2) + 100;
            hitResults = new HitResult[screenResolution];
            MouseMove += Form1_MouseMove;
            prevMousePos = Cursor.Position;
            g = Graphics.FromImage(render);
        }
        Point prevMousePos = Point.Empty;
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!prevMousePos.IsEmpty)
            {
                lookAngle -= (e.Location.X - prevMousePos.X) / 200f;
            }

            Cursor.Position = PointToScreen(new Point(Width / 2, Height / 2));
            prevMousePos = new Point(Width / 2, Height / 2);
        }
        Vector2 EstimateNormal(Vector2 pos)
        {
            return Vector2.Normalize(new Vector2(
                signedDstToScene(new Vector2(pos.X + minDst, pos.Y)) - signedDstToScene(new Vector2(pos.X - minDst, pos.Y)),
                signedDstToScene(new Vector2(pos.X, pos.Y + minDst)) - signedDstToScene(new Vector2(pos.X, pos.Y - minDst))
            ));
        }
        Vector2 gravity = Vector2.UnitY * 2f;
        float angleIncrement = fov / screenResolution;

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Dampening effect to gradually reduce velocity
            velocity.X *= .9f;
            velocity.Y *= .9f;
            float speed = .5f;
            // Stop movement if velocity is below a threshold
            if (Math.Abs(velocity.X) < .25f)
            {
                velocity.X = 0;
            }
            if (Math.Abs(velocity.Y) < .25f)
            {
                velocity.Y = 0;
            }

            // Forward and Backward movement
            if (up && velocity.Length() < maxspeed)  // velocity.Length() gives the magnitude of the velocity vector
            {
                velocity.X += (float)Math.Sin(lookAngle) * speed;
                velocity.Y += (float)Math.Cos(lookAngle) * speed;
            }
            else if (down && velocity.Length() < maxspeed)
            {
                velocity.X -= (float)Math.Sin(lookAngle) * speed;
                velocity.Y -= (float)Math.Cos(lookAngle) * speed;
            }

            // Strafing (taking the perpendicular to the look direction)
            if (left && velocity.Length() < maxspeed)
            {
                velocity.X += (float)Math.Sin(lookAngle + Math.PI / 2) * speed; // move left
                velocity.Y += (float)Math.Cos(lookAngle + Math.PI / 2) * speed;
            }
            else if (right && velocity.Length() < maxspeed)
            {
                velocity.X += (float)Math.Sin(lookAngle - Math.PI / 2) * speed; // move right
                velocity.Y += (float)Math.Cos(lookAngle - Math.PI / 2) * speed;
            }
            if (lookLeft)
            {
                lookAngle += 0.05f;
            }
            if (lookRight)
            {
                lookAngle -= 0.05f;
            }
            HandleCollisions();
            Text = player.Position.ToString() + " - lookAngle: " + lookAngle;


            hitResults = new HitResult[screenResolution];

            Parallel.For(-screenResolution / 2, screenResolution / 2, i =>
            {
                float offsetAngle = i * angleIncrement;
                Ray ray = new Ray
                {
                    origin = player.Position,
                    direction = Vector2.Normalize(new Vector2((float)Math.Sin(lookAngle + offsetAngle), (float)Math.Cos(lookAngle + offsetAngle)))
                };
                hitResults[i + screenResolution / 2] = March(ray);
            });

            // Rendering

            Pen p = new Pen(Color.White);
            //g.FillRectangle(Brushes.Gray, 0, 0, render.Width, render.Height);
            g.FillRectangle(ceilingColor, 0, 0, render.Width, render.Height / 2);
            g.FillRectangle(floorColor, 0, render.Height / 2, render.Width, render.Height);
            for (int i = 0; i < hitResults.Length; i++)
            {
                p.Color = hitResults[i].Color;
                var centralLookAngle = lookAngle + (fov / 2);  // Move the lookAngle to the center
                var rayAngle = centralLookAngle + (i - screenResolution) * angleIncrement;
                float fisheyeCorrection = (float)Math.Cos(rayAngle - lookAngle);
                float rawDist = (float)GetDistance(player.Position.X, player.Position.Y, hitResults[i].EndPoint.X, hitResults[i].EndPoint.Y);
                float correctedDist = rawDist * fisheyeCorrection;

                float len = (1 / correctedDist) * 5000;
                if (float.IsInfinity(len))
                {
                    continue;
                }
                float wallStartY = (render.Height / 2) - len;
                float wallEndY = (render.Height / 2) + len;
                g.DrawLine(p, i, (render.Height / 2) - len, i, (render.Height / 2) + len);
                //g.DrawLine(floorColor, i, (render.Height / 2) + len, i, render.Height - 1);
                //g.DrawLine(ceilingColor, i, 0, i, (render.Height / 2) - len);
            }

            // Not sure what theta does, but keeping it
            theta += 0.01f;

            // Redraw
            Invalidate();
        }

        Vector2 CalculateTotalCorrection(List<ShapeCollisionInfo> collisions)
        {
            Vector2 totalCorrection = Vector2.Zero;
            foreach (var collision in collisions)
            {
                float weight = 1.0f / (collision.SignedDistance + 0.0000001f); // Adding a small value to prevent division by zero
                totalCorrection += collision.Normal * (player.Size.X - collision.SignedDistance) * weight;
            }
            return totalCorrection;
        }
        void HandleCollisions()
        {
            const int maxIterations = 10;
            int currentIteration = 0;

            Vector2 predictedPosition = player.Position + velocity;

            while (currentIteration < maxIterations)
            {
                List<ShapeCollisionInfo> collisions = GetCollidingShapes(predictedPosition, player.Size.X + minDst);

                if (collisions.Count == 0) // No more collisions to resolve
                    break;

                Vector2 totalCorrection = CalculateTotalCorrection(collisions);
                predictedPosition += totalCorrection;

                currentIteration++;
            }

            if (currentIteration > 0) // If we went through any iteration, it means there was a collision.
            {
                velocity = Vector2.Zero;
            }

            player.Position = predictedPosition;
        }
        struct ShapeCollisionInfo
        {
            public float SignedDistance;
            public Vector2 Normal;
        }

        List<ShapeCollisionInfo> GetCollidingShapes(Vector2 position, float threshold)
        {
            List<ShapeCollisionInfo> collisions = new List<ShapeCollisionInfo>();
            foreach (var item in shapes)
            {
                float dist = item.Distance(position);
                if (dist < threshold)
                {
                    Vector2 norm = EstimateNormal(position); // Assuming EstimateNormal uses the position to estimate the normal
                    collisions.Add(new ShapeCollisionInfo { SignedDistance = dist, Normal = norm });
                }
            }
            return collisions;
        }
        Brush floorColor = new SolidBrush(Color.DarkGray);
        Brush ceilingColor = new SolidBrush(Color.LightBlue);
        Color fogColor = Color.Transparent;
        float fogStart = 75f; // Fog starts to appear at this distance
        float fogEnd = 150f; // Objects are fully obscured by fog beyond this distance
        private HitResult March(Ray r, float? minimumDst = null)
        {
            float mDst = minDst;
            if (minimumDst != null)
            {
                mDst = minimumDst.Value;
            }
            Color currentCol = new Color();
            float rayDst = 0;
            float circleSize = 0;
            int iter = 0;

            // Define the light direction here
            Vector2 lightDirection = new Vector2(0, -1);
            lightDirection = Vector2.Normalize(lightDirection);

            while (rayDst <= maxDst && iter < maxSteps)
            {
                circleSize = signedDstToScene(r.origin, ref currentCol);
                rayDst += circleSize;

                if (circleSize > mDst)
                {
                    r.origin += r.direction * circleSize;
                }
                else
                {
                    #region Static Light
                    Vector2 wallNormal = EstimateNormal(r.origin);
                    // Calculate the illumination based on the wall's normal and light direction
                    float illumination = Vector2.Dot(lightDirection, wallNormal) * 2;
                    illumination = Clamp(illumination, 0.7f, 1f);
                    #endregion

                    // Adjust the color based on illumination
                    currentCol = AdjustColor(currentCol, illumination); // Implement this function
                    #region Fog
                    //float fogFactor = (rayDst - fogStart) / (fogEnd - fogStart);
                    //fogFactor = Math.Max(0, Math.Min(1, fogFactor)); // Clamp between 0 and 1
                    //Color finalColor = BlendColors(currentCol, fogColor, fogFactor);
                    #endregion
                    return new HitResult
                    {
                        Color = currentCol,
                        EndPoint = r.origin,
                        distanceToScene = signedDstToScene(r.origin),
                    };
                }

                iter++;
            }

            return new HitResult
            {
                Color = fogColor,
                EndPoint = r.origin,
                distanceToScene = signedDstToScene(r.origin),
            };
        }

        // Implement this function to adjust color based on illumination
        private Color AdjustColor(Color original, float illumination)
        {
            // Scale each color component by illumination
            int r = (int)(original.R * illumination);
            int g = (int)(original.G * illumination);
            int b = (int)(original.B * illumination);

            return Color.FromArgb(r, g, b);
        }
        Color BlendColors(Color baseColor, Color blendColor, float amount)
        {
            byte r = (byte)(baseColor.R + (blendColor.R - baseColor.R) * amount);
            byte g = (byte)(baseColor.G + (blendColor.G - baseColor.G) * amount);
            byte b = (byte)(baseColor.B + (blendColor.B - baseColor.B) * amount);
            return Color.FromArgb(r, g, b);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            foreach (var item in shapes)
            {
                if (item is Circle)
                {
                    e.Graphics.DrawEllipse(Pens.Red, item.Position.X - item.Size.X, item.Position.Y - item.Size.X, item.Size.X * 2, item.Size.X * 2);
                }
                else if (item is Box)
                {
                    e.Graphics.DrawRectangle(Pens.Blue, item.Position.X - item.Size.X, item.Position.Y - item.Size.Y, item.Size.X * 2, item.Size.Y * 2);
                }
                else if (item is StarFive)
                {
                    //cant draw star simply lol
                }
            }
            try
            {
                for (int i = 0; i < hitResults.Length; i++)
                {
                    HitResult result = hitResults[i];
                    if (result == null)
                    {
                        continue;
                    }
                    var centralLookAngle = lookAngle + (fov / 2);
                    float rayAngle = centralLookAngle + (i - screenResolution) * angleIncrement;

                    // Calculate dot product for Fresnel effect
                    Vector2 viewDirection = new Vector2((float)Math.Cos(lookAngle), (float)Math.Sin(lookAngle));
                    Vector2 rayDirection = new Vector2((float)Math.Cos(rayAngle), (float)Math.Sin(rayAngle));
                    float dotProduct = Vector2.Dot(viewDirection, rayDirection);
                    // Fresnel calculation
                    float power = 0.6f; // Adjust this as needed
                    float fresnel = 1.0f-Clamp((float)Math.Pow(1.0 - Math.Abs(dotProduct), power), 0, 1);
                    // Adjust color brightness based on Fresnel effect
                    Color wallColor = ColorMultiply(hitResults[i].Color, fresnel);
                    e.Graphics.DrawLine(Pens.Green, result.EndPoint.X, result.EndPoint.Y, player.Position.X, player.Position.Y);

                    float fisheyeCorrection = (float)Math.Cos(rayAngle - lookAngle);
                    float rawDist = (float)GetDistance(player.Position.X, player.Position.Y, hitResults[i].EndPoint.X, hitResults[i].EndPoint.Y);
                    float correctedDist = rawDist * fisheyeCorrection;

                    float len = (1 / correctedDist) * 5000;
                    g.DrawLine(new Pen(wallColor, 1), i, (render.Height / 2) - len, i, (render.Height / 2) + len);
                }
            }
            catch (Exception)
            {

            }
            //e.Graphics.DrawEllipse(Pens.Black, Cursor.Position.X - circleSize - Left, Cursor.Position.Y - circleSize - Top, circleSize * 2, circleSize * 2);
            e.Graphics.DrawImageUnscaled(render, ClientRectangle.X, formHeight);
            e.Graphics.DrawEllipse(Pens.Red, player.Position.X - player.Size.X, player.Position.Y - player.Size.X, player.Size.X * 2, player.Size.X * 2);

        }
        private Color ColorMultiply(Color color, float factor)
        {
            int r = (int)(color.R * factor);
            int g = (int)(color.G * factor);
            int b = (int)(color.B * factor);
            return Color.FromArgb(Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }
        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                up = false;
            }
            else if (e.KeyCode == Keys.S)
            {
                down = false;
            }
            if (e.KeyCode == Keys.A)
            {
                right = false;
            }
            else if (e.KeyCode == Keys.D)
            {
                left = false;
            }
            if (e.KeyCode == Keys.E)
            {
                lookRight = false;
            }
            if (e.KeyCode == Keys.Q)
            {
                lookLeft = false;
            }
        }
        bool lookLeft, lookRight;
        float lookAngle = 0f;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                up = true;
            }
            else if (e.KeyCode == Keys.S)
            {
                down = true;
            }
            if (e.KeyCode == Keys.A)
            {
                right = true;
            }
            else if (e.KeyCode == Keys.D)
            {
                left = true;
            }
            if (e.KeyCode == Keys.E)
            {
                lookRight = true;
            }
            if (e.KeyCode == Keys.Q)
            {
                lookLeft = true;
            }
            if (e.KeyCode == Keys.P)
            {
                render.Save("render.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
