using System;
using System.ComponentModel;
using Windows.Devices.Gpio;

namespace Matrix8x8
{
    public class Driver2x74HC595
    {
        #region DataMembers

        private GpioPin PinDS { get; set; }
        private GpioPin PinSTCP { get; set; }
        private GpioPin PinSHCP { get; set; }

        private bool m_IsScanActive = false;
        private byte[] m_ActiveMatrix = new byte[8];  // latch for the current presenting matrix 8x8
        BackgroundWorker m_WorkerThread;
        private static readonly object m_ThreadLock = new object(); // lock object to start/stop scanning thread

        #endregion

        /// <summary>
        /// Create a new instance of Driver2x74HC595
        /// </summary>
        /// <param name="DsPin">Ds Pin Number</param>
        /// <param name="STcpPin">STcp Pin Number</param>
        /// <param name="SHcpPin">SHcp Pin Number</param>
        public Driver2x74HC595(int DsPin, int STcpPin, int SHcpPin)
        {
            m_IsScanActive = false;

            var gpio = GpioController.GetDefault();

            // setup the pins
            PinDS = gpio.OpenPin(DsPin);
            PinDS.SetDriveMode(GpioPinDriveMode.Output);

            PinSTCP = gpio.OpenPin(STcpPin);
            PinSTCP.SetDriveMode(GpioPinDriveMode.Output);

            PinSHCP = gpio.OpenPin(SHcpPin);
            PinSHCP.SetDriveMode(GpioPinDriveMode.Output);

            // initialize the pins to low
            PinDS.Write(GpioPinValue.Low);
            PinSTCP.Write(GpioPinValue.Low);
            PinSHCP.Write(GpioPinValue.Low);

            // create scan thread
            m_WorkerThread = new BackgroundWorker { WorkerSupportsCancellation = true };
            m_WorkerThread.DoWork += (sender, args) =>
            {
                while (!m_WorkerThread.CancellationPending)
                {
                    PresentMatrix();    // scan matrix to LED
                }
            };
        }


        #region Interface


        // latch 8x8 matrix and start (or continue) showing it with LED
        public void ShowMatrix(byte[,] _matrix)
        {
            if (_matrix.Length != m_ActiveMatrix.Length * 8)
            {
                throw new ArgumentException(string.Format("Invalid length: should be {0} instead of {1} ", m_ActiveMatrix.Length * 8, _matrix.Length));
            }

            // copy 8x8 matrix to 8 byte array
            byte[] bitArray = new byte[8];  // temporary array to create bytes
            for (int i = 0, ei = 8; i < ei; ++i)
            {
                for (int j = 0, ej = bitArray.Length; j < ej; ++j)
                {
                    bitArray[j] = _matrix[i, j];
                }
                byte val = GetByteOf8Bits(bitArray);
                m_ActiveMatrix[i] = val;
            }

            if (!m_IsScanActive)
            {
                StartShowMatrix();  // possible start scanning
            }
        }



        // latch 8bytes matrix and start (or continue) showing it with LED
        public void ShowMatrix(byte[] _matrix)
        {
            if (_matrix.Length != m_ActiveMatrix.Length)
            {
                throw new ArgumentException(string.Format("Invalid length: should be {0} instead of {1} ", m_ActiveMatrix.Length, _matrix.Length));
            }

            m_ActiveMatrix = _matrix;   // one-time copy. Do not protect with lock as anyway scan is real-time.

            if (!m_IsScanActive)
            {
                StartShowMatrix();  // possible start scanning
            }
        }


        public void StopShowMatrix()
        {
            if (m_IsScanActive)
            {
                lock (m_ThreadLock)
                {
                    if (m_IsScanActive)
                    {
                        m_WorkerThread.CancelAsync();
                        m_IsScanActive = false;
                    }
                }
            }
        }


        // clear matrix
        public void CleanMatrix(byte _cleanWith)
        {
            for (int i = 0, ei = 8; i < ei; ++i)
            {
                for (int j = 0, ej = 8; j < ej; ++j)
                {
                    m_ActiveMatrix[i] = _cleanWith;
                }
            }
        }


        #endregion


        #region Presenting


        private void StartShowMatrix()
        {
            if (!m_IsScanActive)
            {
                lock (m_ThreadLock)
                {
                    if (!m_IsScanActive)
                    {
                        m_IsScanActive = true;
                        m_WorkerThread.RunWorkerAsync();
                    }
                }
            }
        }


        // show full matrix to LED
        private void PresentMatrix()
        {
            for (int i = 0, ei = 8; i < ei; ++i)
            {
                DisplayChar(i, m_ActiveMatrix[i]);
            }
        }


        #endregion

        
        // get byte of 8 bits (represented as ints)
        private byte GetByteOf8Bits(byte[] _8bits)
        {
            byte val = 0;
            byte mask = 0x01;
            for (int j = 0, ej = 8; j < ej; ++j)
            {
                if (_8bits[j] == 1)
                {
                    val |= mask;
                }
                mask <<= 1;
            }

            return val;
        }


        #region SPI

        // present one row at 8x8 matrix
        private void DisplayChar(int rowIndex, byte value8Leds)
        {
            byte i = 0x01;
            i = (byte)(i << rowIndex);

            WriteData(i);     // output row index (most significant 74HC595)
            WriteData(value8Leds);  // output row led data (less significant 74HC595)
            PulseSTCP();            // present row
        }

        // Write one byte of data
        private void WriteData(byte rowData8Bit)
        {
            for (int i = 8; i >= 1; i--)
            {
                PinDS.Write((rowData8Bit & 0x80) > 0 ? GpioPinValue.High : GpioPinValue.Low);
                rowData8Bit <<= 1;
                PulseSHCP();
            }
        }


        // Pulse "storage register clock input"
        private void PulseSTCP()
        {
            PinSTCP.Write(GpioPinValue.Low);
            PinSTCP.Write(GpioPinValue.High);
        }


        // Pulse "shift register clock input"
        private void PulseSHCP()
        {
            PinSHCP.Write(GpioPinValue.Low);
            PinSHCP.Write(GpioPinValue.High);
        }

        #endregion

    }
}
