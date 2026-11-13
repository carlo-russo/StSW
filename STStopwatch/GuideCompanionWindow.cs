using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace STStopwatch
{
    /// <summary>A draggable, independent desktop companion for Toki, the StartToday focus guide.</summary>
    internal sealed class GuideCompanionWindow : Window
    {
        private readonly TextBlock _quoteText;
        private readonly Border _quoteBubble;
        private readonly Thumb _resizeGrip;
        private readonly DispatcherTimer _quoteTimer;
        private readonly string[] _quotes =
        {
            "One step at a time.", "Small steps add up.", "You are in your flow!",
            "Make this moment count.", "Breathe. Begin again.", "Progress loves focus.",
            "You have got this!", "Keep showing up."
        };
        private int _quoteIndex;

        public GuideCompanionWindow()
        {
            Title = "Toki — StartToday Guide";
            Width = 168;
            Height = 220;
            MinWidth = 168;
            MinHeight = 220;
            MaxWidth = 420;
            MaxHeight = 550;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = true;
            Topmost = true;

            var root = new Canvas { Width = Width, Height = Height, Background = Brushes.Transparent };
            root.MouseLeftButtonDown += OnDragRequested;

            _quoteText = new TextBlock
            {
                Text = "“" + _quotes[0] + "”", FontFamily = new FontFamily("Segoe UI"), FontSize = 11,
                FontWeight = FontWeights.SemiBold, FontStyle = FontStyles.Italic,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { BlurRadius = 4, ShadowDepth = 1, Opacity = .9, Color = Color.FromRgb(15, 25, 45) },
                RenderTransform = new TranslateTransform()
            };
            _quoteText.Background = Brushes.Transparent;
            _quoteBubble = new Border { Width = 156, Child = _quoteText, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Padding = new Thickness(0) };
            Canvas.SetLeft(_quoteBubble, 6);
            Canvas.SetTop(_quoteBubble, 5);
            root.Children.Add(_quoteBubble);

            var viewport = Create3DGuide();
            viewport.Width = 156;
            viewport.Height = 160;
            Canvas.SetLeft(viewport, 6);
            Canvas.SetTop(viewport, 49);
            root.Children.Add(viewport);

            _resizeGrip = new Thumb
            {
                Width = 22, Height = 22, Cursor = Cursors.SizeNWSE,
                Background = Brushes.Transparent, ToolTip = "Drag to enlarge Toki"
            };
            _resizeGrip.DragDelta += OnResizeGripDragDelta;
            Canvas.SetLeft(_resizeGrip, root.Width - _resizeGrip.Width);
            Canvas.SetTop(_resizeGrip, root.Height - _resizeGrip.Height);
            Panel.SetZIndex(_resizeGrip, 5);
            root.Children.Add(_resizeGrip);

            Content = new Viewbox { Stretch = Stretch.Uniform, Child = root };

            _quoteTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(3.6) };
            _quoteTimer.Tick += OnQuoteTimerTick;
            _quoteTimer.Start();
            Closed += delegate { _quoteTimer.Stop(); };
        }

        private void OnDragRequested(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (ReferenceEquals(source, _resizeGrip)) return;
                source = VisualTreeHelper.GetParent(source);
            }
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch (InvalidOperationException) { }
            }
        }

        private void OnResizeGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            double scale = Math.Min(Width / 168.0, Height / 220.0);
            Width = Math.Max(MinWidth, Math.Min(MaxWidth, Width + e.HorizontalChange * scale));
            Height = Math.Max(MinHeight, Math.Min(MaxHeight, Height + e.VerticalChange * scale));
        }

        private void OnQuoteTimerTick(object sender, EventArgs e)
        {
            _quoteIndex = (_quoteIndex + 1) % _quotes.Length;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
            fade.Completed += delegate
            {
                _quoteText.Text = "“" + _quotes[_quoteIndex] + "”";
                var move = _quoteBubble.RenderTransform as TranslateTransform;
                if (move != null)
                {
                    move.Y = 7;
                    move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(7, 0, TimeSpan.FromMilliseconds(520))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                _quoteBubble.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360))
                { EasingFunction = new SineEase() });
            };
            _quoteBubble.BeginAnimation(OpacityProperty, fade);
        }

        private static Viewport3D Create3DGuide()
        {
            var viewport = new Viewport3D { ClipToBounds = true };
            viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, .95, 6.4), LookDirection = new Vector3D(0, 0, -6.4),
                UpDirection = new Vector3D(0, 1, 0), FieldOfView = 29
            };
            viewport.Children.Add(new ModelVisual3D
            {
                Content = new AmbientLight(Color.FromRgb(150, 159, 181))
            });
            viewport.Children.Add(new ModelVisual3D
            {
                Content = new DirectionalLight(Colors.White, new Vector3D(-.25, -.55, -1))
            });
            viewport.Children.Add(new ModelVisual3D
            {
                Content = new DirectionalLight(Color.FromRgb(100, 151, 255), new Vector3D(.8, -.1, .6))
            });

            var bobRoot = new ModelVisual3D();
            var bob = new TranslateTransform3D();
            bobRoot.Transform = bob;
            viewport.Children.Add(bobRoot);

            var turnRoot = new ModelVisual3D();
            var yaw = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -7);
            turnRoot.Transform = new RotateTransform3D(yaw, new Point3D(0, 1.15, 0));
            bobRoot.Children.Add(turnRoot);

            var blue = MakeMaterial(Color.FromRgb(0, 82, 255));
            var lightBlue = MakeMaterial(Color.FromRgb(75, 150, 255));
            var ink = MakeMaterial(Color.FromRgb(18, 28, 46));
            var white = MakeMaterial(Colors.White);
            var blush = MakeMaterial(Color.FromRgb(190, 94, 94));
            var skin = MakeMaterial(Color.FromRgb(128, 70, 45));
            var suit = MakeMaterial(Color.FromRgb(24, 43, 76));
            var lapel = MakeMaterial(Color.FromRgb(42, 67, 108));
            var trousers = MakeMaterial(Color.FromRgb(31, 48, 78));
            var tie = MakeMaterial(Color.FromRgb(0, 112, 222));

            // Tailored jacket, tapered trousers, shirt front, lapels, and tie.
            AddTailoredTorso(turnRoot, suit);
            AddEllipsoid(turnRoot, 0, .67, 0, .34, .32, .29, trousers);
            AddEllipsoid(turnRoot, 0, 1.72, 0, .135, .18, .14, skin);
            AddEllipsoid(turnRoot, 0, 1.22, .30, .17, .39, .025, white);
            AddTriangle(turnRoot, new Point3D(0, 1.60, .34), new Point3D(-.24, 1.47, .34),
                new Point3D(-.09, 1.10, .37), lapel);
            AddTriangle(turnRoot, new Point3D(0, 1.60, .34), new Point3D(.24, 1.47, .34),
                new Point3D(.09, 1.10, .37), lapel);
            AddEllipsoid(turnRoot, 0, 1.48, .35, .065, .065, .026, tie);
            AddEllipsoid(turnRoot, 0, 1.28, .345, .052, .18, .022, tie);
            AddEllipsoid(turnRoot, 0, 1.08, .34, .055, .075, .025, tie);
            AddEllipsoid(turnRoot, .20, 1.22, .33, .025, .025, .02, white);
            AddEllipsoid(turnRoot, .20, 1.08, .33, .025, .025, .02, white);
            // StartToday pin on the left lapel.
            AddEllipsoid(turnRoot, -.29, 1.39, .34, .11, .11, .045, white);
            AddEllipsoid(turnRoot, -.29, 1.39, .37, .095, .095, .035, blue);
            AddPinText(turnRoot, white);

            var leftArm = AddArm(turnRoot, -.42, 1.55, true, suit, lapel, skin, ink, white);
            var rightArm = AddArm(turnRoot, .42, 1.55, false, suit, lapel, skin, ink, white);
            var leftLeg = AddLeg(turnRoot, -.22, .52, true, suit, lapel, ink);
            var rightLeg = AddLeg(turnRoot, .22, .52, false, suit, lapel, ink);

            // Head has its own gentle nod, connected through a neck pivot.
            var headPivot = new ModelVisual3D { Transform = new TranslateTransform3D(0, 1.82, 0) };
            var nod = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            var head = new ModelVisual3D { Transform = new RotateTransform3D(nod) };
            headPivot.Children.Add(head);
            turnRoot.Children.Add(headPivot);
            AddShapedHead(head, skin);
            // Ears, swept hair, and side locks give the head a clearer silhouette.
            AddEllipsoid(head, -.37, .40, .01, .095, .14, .09, skin);
            AddEllipsoid(head, .37, .40, .01, .095, .14, .09, skin);
            AddEllipsoid(head, 0, .65, -.005, .385, .17, .33, ink);
            AddEllipsoid(head, -.20, .61, .23, .19, .11, .12, ink);
            AddEllipsoid(head, -.39, .37, .015, .035, .08, .035, blush);
            AddEllipsoid(head, .39, .37, .015, .035, .08, .035, blush);
            // Eyebrows, bright eye whites, pupils, and catchlights.
            AddEllipsoid(head, -.14, .56, .28, .10, .027, .035, ink);
            AddEllipsoid(head, .14, .56, .28, .10, .027, .035, ink);
            AddEllipsoid(head, -.14, .44, .29, .082, .10, .045, white);
            AddEllipsoid(head, .14, .44, .29, .082, .10, .045, white);
            AddEllipsoid(head, -.13, .44, .329, .045, .065, .03, ink);
            AddEllipsoid(head, .15, .44, .329, .045, .065, .03, ink);
            AddEllipsoid(head, -.145, .47, .354, .018, .024, .014, white);
            AddEllipsoid(head, .135, .47, .354, .018, .024, .014, white);
            // Rounded bridge and tip, plus an open smile with visible teeth.
            AddEllipsoid(head, 0, .37, .315, .055, .10, .07, skin);
            AddEllipsoid(head, 0, .31, .365, .075, .052, .065, skin);
            AddEllipsoid(head, -.24, .29, .19, .065, .035, .016, blush);
            AddEllipsoid(head, .24, .29, .19, .065, .035, .016, blush);
            AddEllipsoid(head, 0, .205, .19, .13, .075, .04, ink);
            AddEllipsoid(head, 0, .238, .224, .09, .024, .016, white);
            AddEllipsoid(head, 0, .17, .224, .065, .02, .014, blush);
            // The loop moves through four different dance poses with eased transitions.
            AnimateDance(bob, yaw, nod, leftArm, rightArm, leftLeg, rightLeg);
            return viewport;
        }

        private static JointRig AddArm(ModelVisual3D parent, double x, double y, bool left,
            Material upperMaterial, Material lowerMaterial, Material handMaterial,
            Material jointMaterial, Material cuffMaterial)
        {
            double direction = left ? 1 : -1;
            const double upperLength = .43;
            const double lowerLength = .39;
            var shoulder = new ModelVisual3D { Transform = new TranslateTransform3D(x, y, 0) };
            var upperPivotAngle = new AxisAngleRotation3D(new Vector3D(0, 0, 1), left ? -18 : 18);
            var upperPivot = new ModelVisual3D { Transform = new RotateTransform3D(upperPivotAngle) };
            shoulder.Children.Add(upperPivot);
            AddEllipsoid(upperPivot, 0, -upperLength / 2, 0, .145, upperLength / 2 + .035, .145, upperMaterial);
            AddEllipsoid(upperPivot, 0, 0, 0, .17, .16, .16, upperMaterial);

            var elbow = new ModelVisual3D { Transform = new TranslateTransform3D(0, -upperLength, 0) };
            upperPivot.Children.Add(elbow);
            AddEllipsoid(elbow, 0, 0, 0, .13, .13, .13, jointMaterial);
            var elbowAngle = new AxisAngleRotation3D(new Vector3D(0, 0, 1), left ? 13 : -13);
            var forearm = new ModelVisual3D { Transform = new RotateTransform3D(elbowAngle) };
            elbow.Children.Add(forearm);
            AddEllipsoid(forearm, 0, -lowerLength / 2, 0, .125, lowerLength / 2 + .035, .13, lowerMaterial);
            AddEllipsoid(forearm, 0, -lowerLength + .015, .005, .135, .07, .14, cuffMaterial);
            // Palm and fingertips overlap the wrist cap, so the hand remains attached in every pose.
            AddEllipsoid(forearm, 0, -lowerLength - .055, .015, .145, .15, .14, handMaterial);
            AddEllipsoid(forearm, -.085, -lowerLength - .13, .045, .052, .085, .07, handMaterial);
            AddEllipsoid(forearm, 0, -lowerLength - .17, .02, .055, .07, .065, handMaterial);
            AddEllipsoid(forearm, .085, -lowerLength - .13, .045, .052, .085, .07, handMaterial);

            parent.Children.Add(shoulder);
            return new JointRig { Primary = upperPivotAngle, Secondary = elbowAngle };
        }

        private static JointRig AddLeg(ModelVisual3D parent, double x, double y, bool left,
            Material upperMaterial, Material lowerMaterial, Material shoeMaterial)
        {
            const double upperLength = .43;
            const double lowerLength = .42;
            var hip = new ModelVisual3D { Transform = new TranslateTransform3D(x, y, 0) };
            var thighAngle = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            var thigh = new ModelVisual3D { Transform = new RotateTransform3D(thighAngle) };
            hip.Children.Add(thigh);
            AddEllipsoid(thigh, 0, -upperLength / 2, 0, .17, upperLength / 2 + .04, .17, upperMaterial);

            var knee = new ModelVisual3D { Transform = new TranslateTransform3D(0, -upperLength, 0) };
            thigh.Children.Add(knee);
            AddEllipsoid(knee, 0, 0, 0, .145, .14, .145, upperMaterial);
            var shinAngle = new AxisAngleRotation3D(new Vector3D(0, 0, 1), left ? 8 : -8);
            var shin = new ModelVisual3D { Transform = new RotateTransform3D(shinAngle) };
            knee.Children.Add(shin);
            AddEllipsoid(shin, 0, -lowerLength / 2, 0, .135, lowerLength / 2 + .035, .14, lowerMaterial);
            AddEllipsoid(shin, left ? -.045 : .045, -lowerLength - .06, .07, .19, .12, .25, shoeMaterial);
            parent.Children.Add(hip);
            return new JointRig { Primary = thighAngle, Secondary = shinAngle };
        }

        private static void AddShapedHead(ModelVisual3D parent, Material material)
        {
            var mesh = new MeshGeometry3D();
            const int rings = 20;
            const int slices = 32;
            for (int ring = 0; ring <= rings; ring++)
            {
                double latitude = Math.PI * ring / rings;
                double vertical = Math.Cos(latitude);
                double sinLatitude = Math.Sin(latitude);
                // Keep the temples broad while tapering into a more defined jaw and chin.
                double shape = vertical < -.32
                    ? .74 + .26 * ((vertical + 1) / .68)
                    : 1.0 - .10 * Math.Max(0, vertical);
                double rx = .39 * shape;
                double rz = .34 * shape;
                for (int slice = 0; slice <= slices; slice++)
                {
                    double longitude = 2 * Math.PI * slice / slices;
                    double nx = sinLatitude * Math.Cos(longitude);
                    double nz = sinLatitude * Math.Sin(longitude);
                    mesh.Positions.Add(new Point3D(rx * nx, .40 + .39 * vertical, rz * nz));
                    var normal = new Vector3D(nx / rx, vertical / .39, nz / rz);
                    normal.Normalize();
                    mesh.Normals.Add(normal);
                }
            }
            AddRingTriangles(mesh, rings, slices);
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            parent.Children.Add(new ModelVisual3D { Content = model });
        }

        private static void AddTailoredTorso(ModelVisual3D parent, Material material)
        {
            // Elliptical cross-sections create sloped shoulders, a fitted waist, and a chest.
            double[] ys = { .80, .94, 1.14, 1.36, 1.52, 1.62 };
            double[] xs = { .27, .34, .42, .47, .49, .30 };
            double[] zs = { .22, .26, .30, .32, .30, .19 };
            const int slices = 32;
            var mesh = new MeshGeometry3D();
            for (int ring = 0; ring < ys.Length; ring++)
            {
                int before = Math.Max(0, ring - 1);
                int after = Math.Min(ys.Length - 1, ring + 1);
                double slopeX = (xs[after] - xs[before]) / (ys[after] - ys[before]);
                double slopeZ = (zs[after] - zs[before]) / (ys[after] - ys[before]);
                for (int slice = 0; slice <= slices; slice++)
                {
                    double angle = 2 * Math.PI * slice / slices;
                    double cosine = Math.Cos(angle);
                    double sine = Math.Sin(angle);
                    mesh.Positions.Add(new Point3D(xs[ring] * cosine, ys[ring], zs[ring] * sine));
                    var normal = new Vector3D(cosine / xs[ring], -(slopeX * cosine * cosine + slopeZ * sine * sine), sine / zs[ring]);
                    normal.Normalize();
                    mesh.Normals.Add(normal);
                }
            }
            for (int ring = 0; ring < ys.Length - 1; ring++)
            {
                int row = ring * (slices + 1);
                int next = (ring + 1) * (slices + 1);
                for (int slice = 0; slice < slices; slice++)
                {
                    int a = row + slice;
                    int b = next + slice;
                    mesh.TriangleIndices.Add(a); mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(a + 1);
                    mesh.TriangleIndices.Add(a + 1); mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(b + 1);
                }
            }
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            parent.Children.Add(new ModelVisual3D { Content = model });
        }

        private static void AddRingTriangles(MeshGeometry3D mesh, int rings, int slices)
        {
            for (int ring = 0; ring < rings; ring++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int a = ring * (slices + 1) + slice;
                    int b = a + slices + 1;
                    mesh.TriangleIndices.Add(a); mesh.TriangleIndices.Add(a + 1); mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(a + 1); mesh.TriangleIndices.Add(b + 1); mesh.TriangleIndices.Add(b);
                }
            }
        }

        private static void AddEllipsoid(ModelVisual3D parent, double x, double y, double z,
            double radiusX, double radiusY, double radiusZ, Material material)
        {
            var mesh = new MeshGeometry3D();
            const int rings = 18;
            const int slices = 28;
            for (int ring = 0; ring <= rings; ring++)
            {
                double latitude = Math.PI * ring / rings;
                double sinLatitude = Math.Sin(latitude);
                double cosLatitude = Math.Cos(latitude);
                for (int slice = 0; slice <= slices; slice++)
                {
                    double longitude = 2 * Math.PI * slice / slices;
                    double nx = sinLatitude * Math.Cos(longitude);
                    double nz = sinLatitude * Math.Sin(longitude);
                    mesh.Positions.Add(new Point3D(x + radiusX * nx, y + radiusY * cosLatitude, z + radiusZ * nz));
                    var normal = new Vector3D(nx / radiusX, cosLatitude / radiusY, nz / radiusZ);
                    normal.Normalize();
                    mesh.Normals.Add(normal);
                }
            }
            AddRingTriangles(mesh, rings, slices);
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            parent.Children.Add(new ModelVisual3D { Content = model });
        }

        private static void AddTriangle(ModelVisual3D parent, Point3D a, Point3D b, Point3D c, Material material)
        {
            var mesh = new MeshGeometry3D();
            var normal = Vector3D.CrossProduct(b - a, c - a);
            if (normal.Z < 0)
            {
                Point3D swap = b;
                b = c;
                c = swap;
                normal = Vector3D.CrossProduct(b - a, c - a);
            }
            normal.Normalize();
            mesh.Positions.Add(a); mesh.Positions.Add(b); mesh.Positions.Add(c);
            mesh.Normals.Add(normal); mesh.Normals.Add(normal); mesh.Normals.Add(normal);
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            parent.Children.Add(new ModelVisual3D { Content = model });
        }

        private static void AddPinText(ModelVisual3D parent, Material material)
        {
            const double z = .409;
            // Tiny raised strokes make the pin read as ST at companion size.
            AddEllipsoid(parent, -.315, 1.423, z, .025, .007, .008, material);
            AddEllipsoid(parent, -.315, 1.390, z, .025, .007, .008, material);
            AddEllipsoid(parent, -.315, 1.357, z, .025, .007, .008, material);
            AddEllipsoid(parent, -.339, 1.373, z, .007, .018, .008, material);
            AddEllipsoid(parent, -.291, 1.407, z, .007, .018, .008, material);
            AddEllipsoid(parent, -.255, 1.423, z, .027, .007, .008, material);
            AddEllipsoid(parent, -.255, 1.390, z, .007, .027, .008, material);
        }

        private static Material MakeMaterial(Color color)
        {
            var group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 24));
            return group;
        }

        private static void AnimateDance(TranslateTransform3D bob, AxisAngleRotation3D bodyTurn,
            AxisAngleRotation3D headNod, JointRig leftArm, JointRig rightArm,
            JointRig leftLeg, JointRig rightLeg)
        {
            // Seven continuous dance variations: groove, side-step, disco reach,
            // twist/wave, cha-cha cross, heel-toe glide, and victory bounce.
            // Every beat moves; the final one resolves into the opening groove.
            AddDanceTrack(bodyTurn, AxisAngleRotation3D.AngleProperty,
                -8, 3, 10, -2, -8, 12, -4, 13, -8, -15, 5, -15, -8, 10, -12, 7, -8,
                8, -6, 12, -8, 16, 2, -15, -8, -12, 5, 14, -8);
            AddDanceTrack(headNod, AxisAngleRotation3D.AngleProperty,
                -2, 6, -4, 4, -2, -5, 2, -6, -2, 7, -4, 7, -2, 4, -6, 2, -2,
                4, -5, 6, -2, -8, -3, 7, -2, 7, 3, -5, -2);
            AddDanceTrack(bob, TranslateTransform3D.OffsetYProperty,
                0, .10, .02, .08, 0, .03, .12, .02, 0, .10, .02, .13, 0, .04, .11, .02, 0,
                .05, .12, .03, 0, .02, .08, .02, 0, .12, .04, .14, 0);

            // Original groove, side-step, disco reach, twist/wave.
            AddDanceTrack(leftArm.Primary, AxisAngleRotation3D.AngleProperty,
                -22, -38, -14, -42, -22, -66, -46, -72, -52, -132, -110, -150, -112, -35, -62, -18, -22,
                -40, -58, -28, -22, -80, -98, -72, -22, -140, -122, -148, -22);
            AddDanceTrack(rightArm.Primary, AxisAngleRotation3D.AngleProperty,
                22, 38, 14, 42, 22, 18, 35, 14, 22, 132, 110, 150, 112, 80, 115, 74, 22,
                40, 58, 28, 22, 80, 98, 72, 22, 140, 122, 148, 22);
            AddDanceTrack(leftArm.Secondary, AxisAngleRotation3D.AngleProperty,
                13, 30, 10, 36, 13, 40, 18, 44, 13, -20, 20, -30, -20, 40, 12, 46, 13,
                28, 52, 14, 13, 20, -8, 24, -13, -45, -20, -52, 13);
            AddDanceTrack(rightArm.Secondary, AxisAngleRotation3D.AngleProperty,
                -13, -30, -10, -36, -13, -8, -35, -5, -13, 20, -20, 30, 13, -45, -10, -50, -13,
                -28, -52, -14, -13, -20, 8, -24, 13, 45, 20, 52, -13);

            AddDanceTrack(leftLeg.Primary, AxisAngleRotation3D.AngleProperty,
                12, -8, 14, -15, 12, -28, -8, -32, 12, 18, -15, 22, 12, -35, -10, -30, 12,
                -28, 18, -32, 12, 25, -12, 34, 12, 20, -22, 28, 12);
            AddDanceTrack(rightLeg.Primary, AxisAngleRotation3D.AngleProperty,
                -12, 8, -14, 15, -12, 20, -8, 24, -12, -18, 15, -22, -12, 25, -15, 30, -12,
                24, -15, 28, -12, -28, 16, -30, -12, -20, 22, -28, -12);
            AddDanceTrack(leftLeg.Secondary, AxisAngleRotation3D.AngleProperty,
                8, 24, 4, 26, 8, 30, 12, 34, 8, -10, 22, -5, 8, 34, 14, 30, 8,
                25, 8, 30, 8, -8, 22, -10, 8, 28, 12, 34, 8);
            AddDanceTrack(rightLeg.Secondary, AxisAngleRotation3D.AngleProperty,
                -8, -24, -4, -26, -8, -12, -30, -10, -8, 10, -22, 5, -8, -30, -14, -34, -8,
                -10, -28, -12, -8, 8, -20, 10, -8, -22, -12, -30, -8);
        }

        private static void AddDanceTrack(Animatable target, DependencyProperty property,
            params double[] poses)
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds((poses.Length - 1) * .525), RepeatBehavior = RepeatBehavior.Forever,
                FillBehavior = FillBehavior.HoldEnd
            };
            for (int i = 0; i < poses.Length; i++)
            {
                double time = i * .525;
                animation.KeyFrames.Add(new SplineDoubleKeyFrame(poses[i],
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(time)))
                {
                    KeySpline = new KeySpline(.2, .1, .8, .9)
                });
            }
            target.BeginAnimation(property, animation);
        }

        private sealed class JointRig
        {
            public AxisAngleRotation3D Primary { get; set; }
            public AxisAngleRotation3D Secondary { get; set; }
        }
    }
}
