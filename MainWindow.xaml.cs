using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LifeSimulation
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private struct Rules
        {
            public string name;
            public bool[] save;
            public bool[] birth;
        }


        private const ushort blocksMinX = 10;
        private const ushort blocksMinY = 10;


        private static readonly Rules lifeRules = new Rules
        {
            name = "Жизнь",
            save = new bool[] { false, false, true, true, false, false, false, false, false, false },
            birth = new bool[] { false, false, false, true, false, false, false, false, false, false },
        };
        private static readonly Rules dayAndNightRules = new Rules
        {
            name = "День Ночь",
            save = new bool[] { false, false, false, true, true, false, true, true, true, false },
            birth = new bool[] { false, false, false, true, false, false, true, true, true, false },
        };
        private static readonly Rules lifeWithoutDeathRules = new Rules
        {
            name = "Без смерти",
            save = new bool[] { true, true, true, true, true, true, true, true, true, true },
            birth = new bool[] { false, false, false, true, false, false, false, false, false, false },
        };
        private static readonly Rules HighLifeRules = new Rules
        {
            name = "HighLife",
            save = new bool[] { false, false, true, true, false, false, false, false, false, false },
            birth = new bool[] { false, false, false, true, false, false, true, false, false, false },
        };
        private static readonly Rules SeedsRules = new Rules
        {
            name = "Семена",
            save = new bool[] { false, false, false, false, false, false, false, false, false, false },
            birth = new bool[] { false, false, true, false, false, false, false, false, false, false },
        };
        private static readonly Rules MorleyRules = new Rules
        {
            name = "Morley",
            save = new bool[] { false, false, true, false, true, true, false, false, false, false },
            birth = new bool[] { false, false, false, true, false, false, true, false, true, false },
        };
        private static Rules OtherRules = new Rules
        {
            name = "Другой",
            save = new bool[] { false, false, false, false, false, false, false, false, false, false },
            birth = new bool[] { false, false, false, false, false, false, false, false, false, false },
        };
        private static readonly Rules[] standartRules = new Rules[]
        {
            lifeRules,
            dayAndNightRules,
            lifeWithoutDeathRules,
            HighLifeRules,
            SeedsRules,
            MorleyRules,
            OtherRules,
        };



        private readonly Brush ON = Brushes.Black;
        private readonly Brush OFF = Brushes.LightGray;
        private readonly DispatcherTimer timer = new DispatcherTimer();


        private ushort blocksMaxX = 90;
        private ushort blocksMaxY = 40;
        private ushort blocksX = 90;
        private ushort blocksY = 40;
        private byte blockSize = 10;
        private int stepCount = 0;

        private bool gameRun = false;

        public Rectangle[,] matrix;
        private Rules rules;


        public MainWindow()
        {
            InitializeComponent();

            Title = "Игра \"жизнь\"";


            CalcBlocksMax();

            tbInputX.Text = blocksMaxX.ToString();
            tbInputY.Text = blocksMaxY.ToString();
            tbPixSize.Text = blockSize.ToString();

            matrix = new Rectangle[blocksY, blocksX];
            rules = standartRules[0];

            foreach (Rules _rule in standartRules)
            {
                _ = cbRules.Items.Add(_rule.name);
            }
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromMilliseconds(1000 - Math.Ceiling(0.0 * 100));
            timer.Tick += GameTick;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Размер top-панели определялся исходя из следующей зависимости:
            // Width = "{Binding WidthWindow, ConverterParameter=-15, Converter={StaticResource WidthWindowConverter}, ElementName=window}"
            // При работе был найден косяк: при разворачивании окна, затем сворачивании, затем восстановлении размер верхней полоски не запоминался и вел себя некорректно
            // Поэтому был реализован данный костыль

            topCanvas.Width = window.Width - 15;
            topPanel.Width = window.Width - 15;


            ResizePlace();
            CalcBlocksMax();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                return;
            }
            if (e.Delta < 0)
            {
                BlocksSizeDown();
            }
            else if (e.Delta > 0)
            {
                BlocksSizeUp();
            }
        }


        #region TopPanel
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void ButtonMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void ButtonMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
        }
        private void ButtonClose_Click(object sender, RoutedEventArgs e) => Close();
        #endregion


        #region Space
        private void GameSpace_Loaded(object sender, RoutedEventArgs e) => BuildBlocks();

        private void ResizePlace()
        {
            gameSpace.Width = blockSize * blocksX <= window.Width - 40 ? blockSize * blocksX : window.Width - 40;
            gameSpace.Height = blockSize * blocksY <= window.Height - 90 ? blockSize * blocksY : window.Height - 90;
        }
        private void ResizeBlocks()
        {
            ResizePlace();

            for (int y = 0; y < blocksY; y++)
            {
                for (int x = 0; x < blocksX; x++)
                {
                    matrix[y, x].Width = blockSize - 1;
                    matrix[y, x].Height = blockSize - 1;
                    Canvas.SetLeft(matrix[y, x], x * blockSize);
                    Canvas.SetTop(matrix[y, x], y * blockSize);
                }
            }

            CalcBlocksMax();
        }
        private void BuildBlocks()
        {
            gameSpace.Children.Clear();
            for (int y = 0; y < blocksY; y++)
            {
                for (int x = 0; x < blocksX; x++)
                {
                    Rectangle r = new Rectangle
                    {
                        Width = blockSize - 1,
                        Height = blockSize - 1,
                        Fill = OFF
                    };
                    _ = gameSpace.Children.Add(r);

                    Canvas.SetLeft(r, x * blockSize);
                    Canvas.SetTop(r, y * blockSize);

                    matrix[y, x] = r;

                    r.MouseDown += R_MouseEnter;
                    r.MouseEnter += R_MouseEnter;
                }
            }
        }
        private void BlocksSizeUp()
        {
            if (blockSize >= 20 || (blockSize + 1) * blocksX > window.Width - 40 || (blockSize + 1) * blocksY > window.Height - 90)
            {
                return;
            }
            blockSize++;
            tbPixSize.Text = blockSize.ToString();
            ResizeBlocks();
        }
        private void BlocksSizeDown()
        {
            if (blockSize <= 5)
            {
                return;
            }
            blockSize--;
            tbPixSize.Text = blockSize.ToString();
            ResizeBlocks();
        }




        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            // Game timer toggle
            if (timer.IsEnabled)
            {
                timer.Stop();
                gameRun = false;
            }
            else
            {
                timer.Start();
                gameRun = true;
            }

            // Change button name
            Button btn = sender as Button;
            btn.Content = gameRun
                ? new Image()
                {
                    Source = new BitmapImage(new Uri(@"Resources/Stop.png", UriKind.Relative)),
                    Height = 12,
                    Width = 12,
                    Margin = new Thickness(-5, 3, -5, 0),
                    ToolTip = "Стоп",
                }
                : new Image()
                {
                    Source = new BitmapImage(new Uri(@"Resources/Start.png", UriKind.Relative)),
                    Height = 12,
                    Width = 12,
                    Margin = new Thickness(-5, 3, -5, 0),
                    ToolTip = "Старт",
                };


            tbInputX.IsReadOnly = gameRun;
            tbInputY.IsReadOnly = gameRun;
            cbRules.IsEnabled = !gameRun;
            tbS.IsReadOnly = gameRun;
            tbB.IsReadOnly = gameRun;
        }
        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (!gameRun)
            {
                Next();
            }
        }
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            if (!gameRun)
            {
                stepCount = 0;
                PrintStep();
                BuildBlocks();
            }
        }
        private void GameSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            double value = slider.Value;

            timer.Interval = TimeSpan.FromMilliseconds(1000 - Math.Ceiling(value * 100));
        }
        private void PixSizeDown_Click(object sender, RoutedEventArgs e)
        {
            BlocksSizeDown();
        }
        private void PixSizeUp_Click(object sender, RoutedEventArgs e)
        {
            BlocksSizeUp();
        }



        private void InputCoordinate_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool _ValidX = ValidNewCoordinateX();
            bool _ValidY = ValidNewCoordinateY();

            try
            {
                tbInputX.Background = _ValidX ? new SolidColorBrush(new Color() { A = 0xFF, R = 0x38, G = 0x38, B = 0x38 }) : Brushes.Red;
                tbInputY.Background = _ValidY ? new SolidColorBrush(new Color() { A = 0xFF, R = 0x38, G = 0x38, B = 0x38 }) : Brushes.Red;
            }
            catch { }
        }
        private void InputCoordinate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbInputX.Text == blocksX.ToString() && tbInputY.Text == blocksY.ToString())
            {
                return;
            }


            if (ValidNewCoordinateX() && ValidNewCoordinateY())
            {
                stepCount = 0;
                PrintStep();

                blocksX = ushort.Parse(tbInputX.Text);
                blocksY = ushort.Parse(tbInputY.Text);
                matrix = new Rectangle[blocksY, blocksX];
                ResizePlace();
                BuildBlocks();
            }
            else
            {
                tbInputX.Text = blocksX.ToString();
                tbInputY.Text = blocksY.ToString();
            }
        }
        private bool ValidNewCoordinateX()
        {
            try
            {
                return blocksMinX <= ushort.Parse(tbInputX.Text) && ushort.Parse(tbInputX.Text) <= blocksMaxX;
            }
            catch { return false; }
        }
        private bool ValidNewCoordinateY()
        {
            try
            {
                return blocksMinY <= ushort.Parse(tbInputY.Text) && ushort.Parse(tbInputY.Text) <= blocksMaxY;
            }
            catch { return false; }
        }


        private void CbRules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Rules _rule in standartRules)
            {
                if (e.AddedItems[0].ToString() == _rule.name)
                {
                    rules = _rule;
                    break;
                }
            }

            tbS.Text = "";
            tbB.Text = "";
            for (int i = 0; i < 9; i++)
            {
                if (rules.save[i])
                {
                    tbS.Text += i.ToString();
                }
                if (rules.birth[i])
                {
                    tbB.Text += i.ToString();
                }
            }
        }
        private void TbRules_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox _textBox = sender as TextBox;

            _textBox.Background = ValidNewRule(_textBox.Text) ? new SolidColorBrush(new Color() { A = 0xFF, R = 0x38, G = 0x38, B = 0x38 }) : Brushes.Red;
        }
        private void TbRules_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox _textBox = sender as TextBox;

            if (ValidNewRule(_textBox.Text))
            {
                for (int i = 0; i < 9; i++)
                {
                    OtherRules.save[i] = tbS.Text.Contains(i.ToString());
                    OtherRules.birth[i] = tbB.Text.Contains(i.ToString());
                }
                rules = OtherRules;
                cbRules.SelectedIndex = cbRules.Items.Count - 1;
            }
            else
            {
                tbS.Text = "";
                tbB.Text = "";
                for (int i = 0; i < 9; i++)
                {
                    if (rules.save[i])
                    {
                        tbS.Text += i.ToString();
                    }
                    if (rules.birth[i])
                    {
                        tbB.Text += i.ToString();
                    }
                }
            }
        }
        private bool ValidNewRule(string _str)
        {
            bool _isValid = 0 < _str.Length && _str.Length <= 9 &&
                              int.TryParse(_str, out _) && !_str.Contains("9");

            for (int i = 0; i < 9; i++)
            {
                if (_str.Contains(i.ToString()))
                {
                    int j = 0;
                    foreach (char _char in _str)
                    {
                        if (_char == i.ToString()[0])
                        {
                            j++;
                            if (2 <= j)
                            {
                                _isValid = false;
                                break;
                            }
                        }
                    }
                }
            }
            return _isValid;
        }



        private void PrintStep() => tbStep.Text = $"Шаг: {(stepCount <= 9999 ? stepCount : 9999)}";
        private void CalcBlocksMax()
        {
            blocksMaxX = (ushort)((ushort)(window.Width - 40) / blockSize);
            blocksMaxY = (ushort)((ushort)(window.Height - 90) / blockSize);
            tbMaxX.Text = $"Xmax: {blocksMaxX}";
            tbMaxY.Text = $"Ymax: {blocksMaxY}";
        }
        #endregion



        #region GamePlay

        private void GameTick(object sender, EventArgs e) => Next();


        private void R_MouseEnter(object sender, MouseEventArgs e)
        {
            tbCurrentX.Text = $"X: {((ushort)Mouse.GetPosition(gameSpace).X / blockSize) + 1}";
            tbCurrentY.Text = $"Y: {((ushort)Mouse.GetPosition(gameSpace).Y / blockSize) + 1}";


            // LeftButton pressed state check
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Pixel definition
                Rectangle pixel = (Rectangle)sender;

                // Draw
                pixel.Fill = pixel.Fill == ON ? OFF : ON;
            }
        }


        private void Next()
        {
            stepCount++;
            PrintStep();
            Trace.WriteLine($"Step =  {stepCount}");

            int[,] neighbors = GetNeighbors();

            for (int y = 0; y < blocksY; y++)
            {
                for (int x = 0; x < blocksX; x++)
                {
                    bool isAlive = matrix[y, x].Fill == ON;

                    if (isAlive && !rules.save[neighbors[y, x]])
                    {
                        matrix[y, x].Fill = OFF;
                    }
                    else if (!isAlive && rules.birth[neighbors[y, x]])
                    {
                        matrix[y, x].Fill = ON;
                    }
                }
            }
        }


        private int[,] GetNeighbors()
        {
            const byte _NEIGHBORS = 8;

            int[,] _matrix = new int[blocksY, blocksX];

            for (int y = 0; y < blocksY; y++)
            {
                for (int x = 0; x < blocksX; x++)
                {
                    int count = 0;

                    for (int i = 0; i < _NEIGHBORS; i++)
                    {
                        int xx;
                        int yy;

                        switch (i)
                        {
                            // Top left
                            case 0:
                                xx = x - 1;
                                yy = y - 1;
                                break;
                            // Top middle
                            case 1:
                                xx = x;
                                yy = y - 1;
                                break;
                            // Top right
                            case 2:
                                xx = x + 1;
                                yy = y - 1;
                                break;
                            // Middle left
                            case 3:
                                xx = x - 1;
                                yy = y;
                                break;
                            // Middle right
                            case 4:
                                xx = x + 1;
                                yy = y;
                                break;
                            // Bottom left
                            case 5:
                                xx = x - 1;
                                yy = y + 1;
                                break;
                            // Bottom middle
                            case 6:
                                xx = x;
                                yy = y + 1;
                                break;
                            // Bottom right
                            case 7:
                                xx = x + 1;
                                yy = y + 1;
                                break;
                            default:
                                xx = x;
                                yy = y;
                                break;
                        }
                        if (ValidCoordinate(xx, yy) && matrix[yy, xx].Fill == ON) count++;
                    }
                    _matrix[y, x] = count;
                }
            }

            return _matrix;
        }


        private bool ValidCoordinate(int x, int y) => 0 <= x && x < blocksX && 0 <= y && y < blocksY;

        #endregion
    }
}
