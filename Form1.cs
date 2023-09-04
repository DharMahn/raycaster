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
        private Bitmap bm;
        private List<float> radii = new List<float>();
        HitResult[] hitResults;
        float maxspeed = 5f;
        private const int max = 20;
        const float fov = (float)Math.PI/3f;
        const int screenResolution = 400;
        //private float circleSize = 0;
        private Random r = new Random();
        private Bitmap render;
        Circle player;
        List<Shape> shapes;
        Vector2 velocity = new Vector2();
        private float theta = 0f;
        const float maxDst = 10000;
        private float minDst = 0.1f;
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
            static float Clamp(float num, float min, float max)
            {
                if (num.CompareTo(min) < 0) return min;
                if (num.CompareTo(max) > 0) return max;
                return num;
            }
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

        private void Form1_Load(object sender, EventArgs e)
        {
            shapes = new List<Shape>();
            Width = formWidth;
            Height = formHeight;
            render = new Bitmap(screenResolution, formHeight);
            player = new Circle
            {
                Position = new Vector2(Width / 2, Height / 2),
                Size = new Vector2(10, 10),
                Color = Color.Purple,
            };

            bm = new Bitmap(Width, Height);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bm.SetPixel(x, y, Color.Transparent);
                }
            }

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
        }
        Vector2 EstimateNormal(Vector2 pos)
        {
            return Vector2.Normalize(new Vector2(
                signedDstToScene(new Vector2(pos.X + minDst, pos.Y)) - signedDstToScene(new Vector2(pos.X - minDst, pos.Y)),
                signedDstToScene(new Vector2(pos.X, pos.Y + minDst)) - signedDstToScene(new Vector2(pos.X, pos.Y - minDst))
            ));
        }
        Vector2 gravity = Vector2.UnitY * 2f;
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Dampening effect to gradually reduce velocity
            velocity.X *= .9f;
            velocity.Y *= .9f;
            float speed = 1;
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
            Ray physicsRay = new Ray
            {
                origin = player.Position,
                direction = Vector2.Normalize(velocity)
            };
            HitResult result = March(physicsRay, player.Size.X);
            if (!float.IsNaN(physicsRay.direction.X) && Vector2.Distance(result.EndPoint, player.Position) <= minDst)
            {
                Vector2 norm = EstimateNormal(result.EndPoint);
                player.Position = result.EndPoint + norm;
                velocity = Vector2.Zero;
            }
            Text = player.Position.ToString() + " - lookAngle: " + lookAngle;


            float angleIncrement = fov / screenResolution;
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
            using (Graphics g = Graphics.FromImage(render))
            {
                g.FillRectangle(Brushes.Gray, 0, 0, render.Width, render.Height);
                for (int i = 0; i < hitResults.Length; i++)
                {
                    var centralLookAngle = lookAngle + (fov / 2);  // Move the lookAngle to the center
                    var rayAngle = centralLookAngle + (i - screenResolution) * angleIncrement;
                    float fisheyeCorrection = (float)Math.Cos(rayAngle - lookAngle);
                    float rawDist = (float)GetDistance(player.Position.X, player.Position.Y, hitResults[i].EndPoint.X, hitResults[i].EndPoint.Y);
                    float correctedDist = rawDist * fisheyeCorrection;

                    float len = (1 / correctedDist) * 5000;
                    g.DrawLine(hitResults[i].Color, i, (render.Height / 2) - len, i, (render.Height / 2) + len);
                }
            }

            // Update player position
            player.Position += velocity;

            // Not sure what theta does, but keeping it
            theta += 0.01f;

            // Redraw
            Invalidate();
        }

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
            while (rayDst <= maxDst)
            {
                circleSize = signedDstToScene(r.origin, ref currentCol);
                rayDst += circleSize;
                if (circleSize > mDst)
                {
                    r.origin += r.direction * circleSize;
                }
                else
                {
                    //r.origin += (r.direction * circleSize);
                    return new HitResult
                    {
                        Color = new Pen(currentCol, 1),
                        EndPoint = r.origin,
                    };

                }
            }
            return new HitResult();
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
                    if (result == null) continue;
                    //asd was null, fuck multithreading
                    e.Graphics.DrawLine(Pens.Green, result.EndPoint.X, result.EndPoint.Y, player.Position.X, player.Position.Y);
                    //e.Graphics.DrawEllipse(Pens.Green, posList[i].X - radii[i], posList[i].Y - radii[i], radii[i] * 2, radii[i] * 2);
                }
            }
            catch (Exception)
            {

            }
            e.Graphics.DrawEllipse(Pens.Green, player.Position.X - player.Size.X, player.Position.Y - player.Size.X, player.Size.X * 2, player.Size.X * 2);
            //e.Graphics.DrawEllipse(Pens.Black, Cursor.Position.X - circleSize - Left, Cursor.Position.Y - circleSize - Top, circleSize * 2, circleSize * 2);
            //e.Graphics.DrawImageUnscaled(bm, 0, 0);
            e.Graphics.DrawImageUnscaled(render, ClientRectangle.X, formHeight);
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
                left = false;
            }
            else if (e.KeyCode == Keys.D)
            {
                right = false;
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
                left = true;
            }
            else if (e.KeyCode == Keys.D)
            {
                right = true;
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
