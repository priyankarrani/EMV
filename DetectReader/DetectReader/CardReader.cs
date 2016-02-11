using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DetectReader
{
    public class CardReader : IDisposable
    {
        bool connected = false;
        private IntPtr context = IntPtr.Zero;
        private IntPtr card = IntPtr.Zero;
        private uint activeProtocol = 0;
        private byte[] atr;
        private ASCIICon availableReaders;
        private BackgroundWorker monitorThread = null;
        private WinConCard.SCARD_READERSTATE[] readerStates = null;

        // Debug
        private bool outputDebugString = true;

        public delegate void CardInsertedEventHandler(string reader, byte[] atr);
        public delegate void CardRemovedEventHandler(string reader);

        public event CardInsertedEventHandler CardInserted = null;
        public event CardRemovedEventHandler CardRemoved = null;

        public CardReader()
        {
            int result = 0;

            result = WinConCard.SCardEstablishContext(WinConCard.Scope.SCARD_SCOCPE_SYSTEM, IntPtr.Zero, IntPtr.Zero, out context);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                Debug.WriteLine(WinConCard.SCardErrorMessage(result));
            }

            byte[] readers = null;
            uint readerCount = 0;
            result = WinConCard.SCardListReaders(context, null, readers, ref readerCount);

            readers = new byte[readerCount];
            result = WinConCard.SCardListReaders(context, null, readers, ref readerCount);
            availableReaders = new ASCIICon(readers);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                Debug.WriteLine(WinConCard.SCardErrorMessage(result));
            }

            //Start a background worker thread which monitors available card readers.
            if ((availableReaders.Count > 0))
            {
                readerStates = new WinConCard.SCARD_READERSTATE[availableReaders.Count];

                for (int i = 0; i < readerStates.Length; i++)
                {
                    readerStates[i].szReader = availableReaders.ToArray()[i];
                }

                monitorThread = new BackgroundWorker();
                monitorThread.WorkerSupportsCancellation = true;
                monitorThread.DoWork += WaitChangeStatus;
                monitorThread.RunWorkerAsync();
            }
        }

        public bool Connect(string reader)
        {
            int result = WinConCard.SCardConnect(context, reader, WinConCard.ShareMode.SCARD_SHARE_SHARED, WinConCard.Protocol.SCARD_PROTOCOL_T0 | WinConCard.Protocol.SCARD_PROTOCOL_T1, ref card, ref activeProtocol);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }
            else
            {
                connected = true;
                atr = GetAnswerToReset();
            }

            return (result == WinConCard.SCARD_S_SUCCESS) ? true : false;
        }

        public bool Disconnect()
        {
            int result = WinConCard.SCardDisconnect(card, WinConCard.Disposition.SCARD_UNPOWER_CARD);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }
            else
            {
                connected = false;
                atr = null;
            }

            return (result == WinConCard.SCARD_S_SUCCESS) ? true : false;
        }

        public IEnumerable<string> Readers
        {
            get
            {
                return availableReaders.ToArray();
            }
        }

        public byte[] ATR
        {
            get
            {
                return atr;
            }
        }

        private byte[] GetAnswerToReset()
        {
            int result = 0;
            byte[] readerName = null;
            uint readerLen = 0;
            uint state = 0;
            uint protocol = 0;
            byte[] atr = null;
            uint atrLen = 0;

            result = WinConCard.SCardStatus(card, readerName, ref readerLen, out state, out protocol, atr, ref atrLen);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }

            readerName = new byte[readerLen];
            atr = new byte[atrLen];
            result = WinConCard.SCardStatus(card, readerName, ref readerLen, out state, out protocol, atr, ref atrLen);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }

            #region Debug output
#if DEBUG
            if (outputDebugString)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < atrLen; i++)
                {
                    sb.AppendFormat("{0:X2}", atr[i]);
                }

                Debug.WriteLine(sb.ToString());
            }
#endif
            #endregion

            ASCIICon msReaderName = new ASCIICon(readerName);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }

            return atr;
        }

        public APDURes Transmit(APDUCmd apdu)
        {
            byte[] recvBuffer = new byte[256];
            int recvLength = recvBuffer.Length;
            IntPtr sendPci = IntPtr.Zero;

            switch ((WinConCard.Protocol)activeProtocol)
            {
                case WinConCard.Protocol.SCARD_PROTOCOL_T0:
                    sendPci = WinConCard.SCARD_PCI_T0;
                    break;
                case WinConCard.Protocol.SCARD_PROTOCOL_T1:
                    sendPci = WinConCard.SCARD_PCI_T1;
                    break;
            }

            #region Debug output
#if DEBUG
            if (outputDebugString)
            {
                StringBuilder sb = new StringBuilder();

                foreach (byte b in apdu.ToArray())
                {
                    sb.AppendFormat("{0:X2}", b);
                }

                Debug.WriteLine(sb.ToString());
            }
#endif
            #endregion

            int result = WinConCard.SCardTransmit(card, sendPci, apdu.ToArray(), apdu.ToArray().Length, IntPtr.Zero, recvBuffer, ref recvLength);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }

            #region Debug output
#if DEBUG
            if (outputDebugString)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < recvLength; i++)
                {
                    sb.AppendFormat("{0:X2}", recvBuffer[i]);
                }

                Debug.WriteLine(sb.ToString());
            }
#endif
            #endregion

            byte[] response = new byte[recvLength];
            Buffer.BlockCopy(recvBuffer, 0, response, 0, recvLength);

            return new APDURes(response);
        }

        private void WaitChangeStatus(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                if (context == IntPtr.Zero)
                {
                    return;
                }

                int result = WinConCard.SCardGetStatusChange(context, 1000, readerStates, readerStates.Length);

                for (int i = 0; i < readerStates.Length; i++)
                {
                    // Check if the state changed from the last time.
                    if ((readerStates[i].dwEventState & (int)WinConCard.CardState.Changed) == (int)WinConCard.CardState.Changed)
                    {
                        // Check what changed
                        WinConCard.CardState state = WinConCard.CardState.None;
                        if ((readerStates[i].dwEventState & (int)WinConCard.CardState.Present) == (int)WinConCard.CardState.Present
                            && (readerStates[i].dwCurrentState & (int)WinConCard.CardState.Present) != (int)WinConCard.CardState.Present)
                        {
                            // The card was inserted                            
                            state = WinConCard.CardState.Present;
                        }
                        else if ((readerStates[i].dwEventState & (int)WinConCard.CardState.Empty) == (int)WinConCard.CardState.Empty
                            && (readerStates[i].dwCurrentState & (int)WinConCard.CardState.Empty) != (int)WinConCard.CardState.Empty)
                        {
                            // The card was removed
                            state = WinConCard.CardState.Empty;
                        }

                        if (state != WinConCard.CardState.None && readerStates[i].dwCurrentState != (int)WinConCard.CardState.None)
                        {
                            switch (state)
                            {
                                case WinConCard.CardState.Present:
                                    if (CardInserted != null)
                                    {
                                        Connect(readerStates[i].szReader);
                                        CardInserted(readerStates[i].szReader, ATR);
                                        Disconnect();
                                    }
                                    break;

                                case WinConCard.CardState.Empty:
                                    if (CardRemoved != null)
                                    {
                                        CardRemoved(readerStates[i].szReader);
                                    }
                                    break;
                            }
                        }

                        // Update the current state for the next time they are checked
                        readerStates[i].dwCurrentState = readerStates[i].dwEventState;
                    }
                }
            }
        }

        public void Dispose()
        {
            monitorThread.CancelAsync();
            monitorThread.Dispose();

            int result = WinConCard.SCardReleaseContext(context);

            if (result != WinConCard.SCARD_S_SUCCESS)
            {
                throw new CardDExcep(result);
            }
        }
    }
}
