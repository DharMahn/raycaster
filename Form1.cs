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
        const float fov = 500f;
        const int raysHalf = 250;
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
            render = new Bitmap(raysHalf * 2, formHeight);
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
                        Position = new Vector2(r.Next(0, Width)),
                        Size = new Vector2(r.Next(5, 40)),
                        Color = Color.Red
                    });
                }
                else if (r.NextDouble() > 0.33)
                {
                    shapes.Add(new Box
                    {
                        Position = new Vector2(r.Next(0, Width)),
                        Size = new Vector2(r.Next(5, 40),r.Next(5,40)),
                        Color = Color.Blue
                    });
                }
                else
                {
                    shapes.Add(new StarFive
                    {
                        Position = new Vector2(r.Next(0, Width)),
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
            hitResults = new HitResult[raysHalf * 2];
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

            //hitResults = new HitResult[raysHalf * 2];
            if (!left && !right)
            {
                velocity.X *= .9f;
            }
            if (Math.Abs(velocity.X) < .25f)
            {
                velocity.X = 0;
            }
            if (!up && !down)
            {
                velocity.Y *= .9f;
            }
            if (Math.Abs(velocity.Y) < .25f)
            {
                velocity.Y = 0;
            }

            if (left && velocity.X >= -maxspeed)
            {
                velocity.X--;
            }
            else if (right && velocity.X <= maxspeed)
            {
                velocity.X++;
            }
            if (up && velocity.Y >= -maxspeed)
            {
                velocity.Y--;
            }
            else if (down && velocity.Y <= maxspeed)
            {
                velocity.Y++;
            }
            Ray physicsRay = new Ray
            {
                origin = player.Position,
                direction = Vector2.Normalize(velocity)
            };
            HitResult result = March(physicsRay);
            if (!float.IsNaN(physicsRay.direction.X) && Vector2.Distance(result.EndPoint, player.Position) - player.Size.X <= minDst)
            {
                Vector2 norm = EstimateNormal(result.EndPoint);
                player.Position = result.EndPoint + (norm);
                velocity = Vector2.Zero;
            }
            Text = player.Position.ToString();

            var a = (float)Math.Atan2(PointToClient(Cursor.Position).X - player.Position.X, PointToClient(Cursor.Position).Y - player.Position.Y);
            #region good ol' reliable
            /*for (int i = -raysHalf; i < raysHalf; i++)
            {
                Color currentCol = new Color();
                float rayDst = 0;
                float circleSize = 0;
                Ray r = new Ray { origin = new Vector2(player.Position.X, player.Position.Y), direction = Vector2.Normalize(new Vector2((float)Math.Sin(a + (i / fov)), (float)Math.Cos(a + (i / fov)))) };
                while (rayDst <= maxDst)
                {
                    circleSize = signedDstToScene(r.origin, ref currentCol);
                    rayDst += circleSize;
                    if (circleSize > minDst)
                    {
                        posList.Add(r.origin);
                        radii.Add(circleSize);
                        r.origin += (r.direction * circleSize);
                    }
                    else
                    {
                        posList.Add(r.origin);
                        radii.Add(circleSize);
                        r.origin += (r.direction * circleSize);
                        endPoints.Add(r.origin);
                        endColors.Add(currentCol);

                        break;
                    }
                }
            }*/
            #endregion

            #region multithreaded
            Parallel.For(-raysHalf, raysHalf, i =>
            {
                Ray ray = new Ray { origin = new Vector2(player.Position.X, player.Position.Y), direction = Vector2.Normalize(new Vector2((float)Math.Sin(a + (i / fov)), (float)Math.Cos(a + (i / fov)))) };
                hitResults[i + raysHalf] = March(ray);
            });

            #endregion
            using (Graphics g = Graphics.FromImage(render))
            {
                g.FillRectangle(Brushes.Gray, 0, 0, render.Width, render.Height);
                foreach(HitResult item in hitResults)
                {
                    float len = (10 / (float)GetDistance(player.Position.X, player.Position.Y, item.EndPoint.X, item.EndPoint.Y)) * 500;

                    g.DrawLine(item.Color, item.ID, (render.Height / 2) - len, item.ID, (render.Height / 2) + len);

                }
                for (int i = 0; i < hitResults.Length; i++)
                {
                    var rayAngle = Math.Atan2(hitResults[i].EndPoint.Y - player.Position.Y, hitResults[i].EndPoint.X - player.Position.X);
                    float dist = (float)GetDistance(player.Position.X, player.Position.Y, hitResults[i].EndPoint.X, hitResults[i].EndPoint.Y);
                    float len = (1 / dist) * 5000;
                    g.DrawLine(hitResults[i].Color, hitResults.Length - i, (render.Height / 2) - len, hitResults.Length - i, (render.Height / 2) + len);
                }
            }
            player.Position += velocity;
            theta += 0.01f;
            Invalidate();

        }

        private HitResult March(Ray r)
        {
            Color currentCol = new Color();
            float rayDst = 0;
            float circleSize = 0;
            while (rayDst <= maxDst)
            {
                circleSize = signedDstToScene(r.origin, ref currentCol);
                rayDst += circleSize;
                if (circleSize > minDst)
                {
                    r.origin += (r.direction * circleSize);
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
            /*foreach (var item in shapes)
            {
                if (item is Circle)
                {
                    e.Graphics.DrawEllipse(Pens.Red, item.Position.X - item.Size.X, item.Position.Y - item.Size.X, item.Size.X * 2, item.Size.X * 2);
                }
                else if (item is Box)
                {
                    e.Graphics.DrawRectangle(Pens.Blue, item.Position.X - item.Size.X, item.Position.Y - item.Size.Y, item.Size.X * 2, item.Size.Y * 2);
                }
            }*/
            //e.Graphics.FillEllipse(Brushes.Turquoise, Cursor.Position.X - Left - 3, Cursor.Position.Y - Top - 3, 6, 6);
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
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());

            }
            e.Graphics.DrawEllipse(Pens.Green, player.Position.X - 5, player.Position.Y - 5, 10, 10);
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
        }
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
            if (e.KeyCode == Keys.P)
            {
                render.Save("render.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
