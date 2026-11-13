using System;
using System.Windows;
using System.Windows.Controls;
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
                Text = _quotes[0], FontFamily = new FontFamily("Segoe UI"), FontSize = 11,
                FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(23, 43, 77)),
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            _quoteBubble = new Border
            {
                Width = 156, MinHeight = 36, Padding = new Thickness(9, 7, 9, 7),
                Background = new SolidColorBrush(Color.FromArgb(248, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 82, 255)),
                BorderThickness = new Thickness(1.2), CornerRadius = new CornerRadius(13),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { BlurRadius = 10, ShadowDepth = 2, Opacity = .16, Color = Color.FromRgb(0, 42, 140) },
                Child = _quoteText,
                RenderTransform = new TranslateTransform()
            };
            Canvas.SetLeft(_quoteBubble, 6);
            Canvas.SetTop(_quoteBubble, 5);
            root.Children.Add(_quoteBubble);

            var viewport = Create3DGuide();
            viewport.Width = 156;
            viewport.Height = 160;
            Canvas.SetLeft(viewport, 6);
            Canvas.SetTop(viewport, 49);
            root.Children.Add(viewport);
            Content = root;

            _quoteTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(3.6) };
            _quoteTimer.Tick += OnQuoteTimerTick;
            _quoteTimer.Start();
            Closed += delegate { _quoteTimer.Stop(); };
        }

        private void OnDragRequested(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch (InvalidOperationException) { }
            }
        }

        private void OnQuoteTimerTick(object sender, EventArgs e)
        {
            _quoteIndex = (_quoteIndex + 1) % _quotes.Length;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
            fade.Completed += delegate
            {
                _quoteText.Text = _quotes[_quoteIndex];
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
            var pale = MakeMaterial(Color.FromRgb(239, 246, 255));
            var ink = MakeMaterial(Color.FromRgb(18, 28, 46));
            var white = MakeMaterial(Colors.White);
            var blush = MakeMaterial(Color.FromRgb(255, 166, 181));

            // Torso, hips, and neck overlap slightly to keep a single, rounded silhouette.
            AddEllipsoid(turnRoot, 0, 1.12, 0, .48, .54, .34, blue);
            AddEllipsoid(turnRoot, 0, .67, 0, .34, .32, .29, lightBlue);
            // A short, clearly visible neck bridges the body and head.
            AddEllipsoid(turnRoot, 0, 1.72, 0, .145, .18, .15, lightBlue);

            // A small raised chest badge keeps the StartToday identity in the 3D version.
            AddEllipsoid(turnRoot, 0, 1.28, .316, .18, .18, .045, pale);
            AddEllipsoid(turnRoot, -.055, 1.28, .36, .025, .09, .018, blue);
            AddEllipsoid(turnRoot, .025, 1.33, .36, .07, .025, .018, blue);
            AddEllipsoid(turnRoot, .025, 1.25, .36, .07, .025, .018, blue);
            AddEllipsoid(turnRoot, .07, 1.29, .36, .025, .065, .018, blue);

            var leftArm = AddArm(turnRoot, -.42, 1.55, true, blue, lightBlue, pale, ink);
            var rightArm = AddArm(turnRoot, .42, 1.55, false, blue, lightBlue, pale, ink);
            var leftLeg = AddLeg(turnRoot, -.22, .52, true, blue, lightBlue, ink);
            var rightLeg = AddLeg(turnRoot, .22, .52, false, blue, lightBlue, ink);

            // Head has its own gentle nod, connected through a neck pivot.
            var headPivot = new ModelVisual3D { Transform = new TranslateTransform3D(0, 1.82, 0) };
            var nod = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            var head = new ModelVisual3D { Transform = new RotateTransform3D(nod) };
            headPivot.Children.Add(head);
            turnRoot.Children.Add(headPivot);
            AddEllipsoid(head, 0, .40, 0, .39, .39, .34, pale);
            AddEllipsoid(head, -.14, .44, .292, .048, .064, .038, ink);
            AddEllipsoid(head, .14, .44, .292, .048, .064, .038, ink);
            AddEllipsoid(head, -.24, .31, .265, .075, .035, .02, blush);
            AddEllipsoid(head, .24, .31, .265, .075, .035, .02, blush);
            AddEllipsoid(head, 0, .27, .31, .12, .027, .027, blue);
            // The loop moves through four different dance poses with eased transitions.
            AnimateDance(bob, yaw, nod, leftArm, rightArm, leftLeg, rightLeg);
            return viewport;
        }

        private static JointRig AddArm(ModelVisual3D parent, double x, double y, bool left,
            Material upperMaterial, Material lowerMaterial, Material handMaterial, Material jointMaterial)
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
            var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
            parent.Children.Add(new ModelVisual3D { Content = model });
        }

        private static Material MakeMaterial(Color color)
        {
            return new DiffuseMaterial(new SolidColorBrush(color));
        }

        private static void AnimateDance(TranslateTransform3D bob, AxisAngleRotation3D bodyTurn,
            AxisAngleRotation3D headNod, JointRig leftArm, JointRig rightArm,
            JointRig leftLeg, JointRig rightLeg)
        {
            // Seven poses, with a short hold and an eased 0.4 second transition
            // between each. The eighth point returns to the groove for a clean loop.
            AddDanceTrack(bodyTurn, AxisAngleRotation3D.AngleProperty,
                -8, 12, -14, 10, -18, 16, -4, -8);
            AddDanceTrack(headNod, AxisAngleRotation3D.AngleProperty,
                -2, -5, 6, -4, 3, -6, 5, -2);
            AddDanceTrack(bob, TranslateTransform3D.OffsetYProperty,
                0, .10, .03, .08, .02, .11, .05, 0);

            // Groove, side-step, disco reach, twist, wave, cross-step, celebration.
            AddDanceTrack(leftArm.Primary, AxisAngleRotation3D.AngleProperty,
                -22, -66, -130, -42, -22, -100, -145, -22);
            AddDanceTrack(rightArm.Primary, AxisAngleRotation3D.AngleProperty,
                22, 18, 130, 42, 110, 62, 145, 22);
            AddDanceTrack(leftArm.Secondary, AxisAngleRotation3D.AngleProperty,
                13, 40, -20, 52, 12, -42, -35, 13);
            AddDanceTrack(rightArm.Secondary, AxisAngleRotation3D.AngleProperty,
                -13, -8, 20, -52, 46, 35, 35, -13);

            AddDanceTrack(leftLeg.Primary, AxisAngleRotation3D.AngleProperty,
                12, -28, 18, -35, 15, -16, 22, 12);
            AddDanceTrack(rightLeg.Primary, AxisAngleRotation3D.AngleProperty,
                -12, 20, -18, 25, -15, 24, -22, -12);
            AddDanceTrack(leftLeg.Secondary, AxisAngleRotation3D.AngleProperty,
                8, 30, -10, 34, 12, 28, 32, 8);
            AddDanceTrack(rightLeg.Secondary, AxisAngleRotation3D.AngleProperty,
                -8, -12, 10, -30, -12, -34, -32, -8);
        }

        private static void AddDanceTrack(Animatable target, DependencyProperty property,
            params double[] poses)
        {
            double[] times = { 0, .8, 1.2, 2, 2.4, 3.2, 3.6, 4.4, 4.8, 5.6, 6, 6.8, 7.2, 8, 8.4 };
            double[] values =
            {
                poses[0], poses[0], poses[1], poses[1], poses[2], poses[2], poses[3], poses[3],
                poses[4], poses[4], poses[5], poses[5], poses[6], poses[6], poses[7]
            };
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(8.4), RepeatBehavior = RepeatBehavior.Forever,
                FillBehavior = FillBehavior.HoldEnd
            };
            for (int i = 0; i < times.Length; i++)
            {
                animation.KeyFrames.Add(new SplineDoubleKeyFrame(values[i],
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(times[i])))
                {
                    KeySpline = new KeySpline(.42, 0, .58, 1)
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
