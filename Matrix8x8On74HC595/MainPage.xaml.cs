using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Matrix8x8On74HC595
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Matrix8x8.Driver2x74HC595 m_Driver2x74HC595;
        private bool m_IsRunning = false;

        public MainPage()
        {
            this.InitializeComponent();

            m_Driver2x74HC595 = new Matrix8x8.Driver2x74HC595(26, 6, 5);

            BackgroundWorker worker = new BackgroundWorker { WorkerSupportsCancellation = true };
            worker.DoWork += (sender, args) =>
            {
                byte[,] matrix = new byte[8, 8];

                CleanMatrix(matrix);

                while (!worker.CancellationPending)
                {
                    if (!m_IsRunning)
                    {
                        continue;
                    }

                    int sec = DateTime.Now.Second;

                    if (sec == 1)
                    {
                        CleanMatrix(matrix);
                    }

                    if (sec == 0)
                    {
                        sec = 60;
                    }
                    else
                    {
                        sec -= 1;
                    }

                    // border 6 x 4
                    if (sec < 6)
                    {
                        matrix[1 + sec, 0] = 0;
                        goto _1;
                    }
                    sec -= 6;

                    if (sec < 6)
                    {
                        matrix[7, 1 + sec] = 0;
                        goto _1;
                    }
                    sec -= 6;
                    if (sec < 6)
                    {
                        matrix[7 - sec - 1, 7] = 0;
                        goto _1;
                    }
                    sec -= 6;
                    if (sec < 6)
                    {
                        matrix[0, 7 - sec - 1] = 0;
                        goto _1;
                    }
                    sec -= 6;

                    int x = 1;
                    int y = 1;
                    for (int i = 5, ei = 1; i >= ei; i -= 2)
                    {
                        if (sec < i)
                        {
                            matrix[x + sec, y] = 0;
                            break;
                        }
                        sec -= i;
                        if (sec < i)
                        {
                            matrix[x + i, y + sec] = 0;
                            break;
                        }
                        sec -= i;
                        if (sec < i)
                        {
                            matrix[x + i - sec, y + i] = 0;
                            break;
                        }
                        sec -= i;
                        if (sec < i)
                        {
                            matrix[x, y + i - sec] = 0;
                            break;
                        }
                        sec -= i;
                        if (sec == 0 && i == 1)
                        {
                            matrix[x, y + i - sec] = 0;
                            break;
                        }
                        x += 1;
                        y += 1;
                    }
                    _1:

                    m_Driver2x74HC595.ShowMatrix(matrix);

                }

            };




            Loaded += (sender, args) =>
            {
                m_IsRunning = true;

                this.StartButton.IsEnabled = false;
                this.StopButton.IsEnabled = true;

                worker.RunWorkerAsync();
            };

        }   // of constructor


        private void CleanMatrix(byte[,] _matrix)
        {
            for (int i = 0, ei = 8; i < ei; ++i)
            {
                for (int j = 0, ej = 8; j < ej; ++j)
                {
                    _matrix[i, j] = 1;
                }
            }
        }


        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            this.StartButton.IsEnabled = false;
            this.StopButton.IsEnabled = true;

            m_IsRunning = true;
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            m_IsRunning = false;

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;

            m_Driver2x74HC595.StopShowMatrix();
        }

    }   // of class MainPage

}   // namespace Matrix8x8On74HC595
