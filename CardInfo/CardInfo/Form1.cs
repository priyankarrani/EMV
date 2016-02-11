using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DetectReader;
using System.Diagnostics; 
using System.Collections.Specialized;


namespace CardInfo
{
    public partial class Form1 : Form
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]

        private static extern int SetWindowTheme(IntPtr hWnd, string appName, string partList);
        private IntPtr context = IntPtr.Zero;
        private CardReader cardReaderList;
        private Thread readThread = null;
        string cdList = "";
        private delegate void UpdateStatusLabelDelegate(string status);
        private delegate void UpdateHourglassDelegate(bool status);

        ASCIIEncoding encoding = new ASCIIEncoding();

        public Form1()
        {
            InitializeComponent(); 
        }
         

        private void Form1_Load(object sender, EventArgs e)
        {       
            getstartD();
        }

        public void getstartD()
        {
            cardReaderList = new CardReader();
            cardReaderList.CardInserted += new CardReader.CardInsertedEventHandler(cardReader_CardInserted);
            cardReaderList.CardRemoved += new CardReader.CardRemovedEventHandler(cardReader_CardRemoved);
            startProc();
           
        }

        private void startProc()
        { 
            foreach (string reader in cardReaderList.Readers)
            {
                cdList = reader;
            }

            if (cdList == "")
            { lblStatus.Text = "Reader not found. Please connect your Reader first."; }
            else {
                UpdateStatusLabel(String.Format("Gathering Information from Card.........."));
                readThread = new Thread(ReadCard);
                readThread.Start(cdList); }
        }
         

        void cardReader_CardInserted(string reader, byte[] atr)
        {
           this.Invoke((MethodInvoker)delegate
             {
                  StringBuilder sb = new StringBuilder();

                foreach (byte b in atr)
                   {
                      sb.AppendFormat("{0:X2}", b);
                  }
                  lblStatus.Text = "Card Inserted. Reading Information.....";
                  startProc();
             });  
        }

        private void UpdateStatusLabel(string status)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = status;
            });
        }

        void cardReader_CardRemoved(string reader)
        {  
            this.Invoke((MethodInvoker)delegate
            {
                lblCHNm.Text = " ";
                lblCardNo.Text = " ";
                lblStatus.Text = "Card Removed.";
            });
             
        } 

        public void ReadCard(object ob)
        {

            string selectedReader = ob as string;
            ASN1 daTa = null;
            List<String[]> la = new List<string[]>();
            List<byte[]> pseIdentifiers = new List<byte[]>();
            List<byte[]> applicationIdentifiers = new List<byte[]>();
            ASCIIEncoding encoding = new ASCIIEncoding();
            APDUCmd apdu = null;
            APDURes response = null;
            bool pseFound = false;

            pseIdentifiers.Add(encoding.GetBytes("1PAY.SYS.DDF01"));
            pseIdentifiers.Add(encoding.GetBytes("2PAY.SYS.DDF01"));

            try
            {
                // Now lets process all Payment System Environments
                if (pseIdentifiers.Count > 0)
                {
                    cardReaderList.Connect(selectedReader);
                    foreach (byte[] pse in pseIdentifiers)
                    {
                        apdu = new APDUCmd(0x00, 0xA4, 0x04, 0x00, pse, (byte)pse.Length);
                        response = cardReaderList.Transmit(apdu);

                        // Get response nescesary
                        if (response.SW1 == 0x61)
                        {
                            apdu = new APDUCmd(0x00, 0xC0, 0x00, 0x00, null, response.SW2);
                            response = cardReaderList.Transmit(apdu);
                        }

                        // PSE application read found ok
                        if (response.SW1 == 0x90)
                        {
                            pseFound = true;
                          daTa = new ASN1(response.Data);
                            byte sfi = new ASN1(response.Data).Find(0x88).Value[0];
                            byte recordNumber = 0x01;
                            byte p2 = (byte)((sfi << 3) | 4);


                            while (response.SW1 != 0x6A && response.SW2 != 0x83)
                            {
                                apdu = new APDUCmd(0x00, 0xB2, recordNumber, p2, null, 0x00);
                                response = cardReaderList.Transmit(apdu);

                                // Retry with correct length
                                if (response.SW1 == 0x6C)
                                {
                                    apdu = new APDUCmd(0x00, 0xB2, recordNumber, p2, null, response.SW2);
                                    response = cardReaderList.Transmit(apdu);
                                }

                                if (response.SW1 == 0x61)
                                {
                                    apdu = new APDUCmd(0x00, 0xC0, 0x00, 0x00, null, response.SW2);
                                    response = cardReaderList.Transmit(apdu);
                                }

                                if (response.Data != null)
                                {                                  
                                    ASN1 aefVal = new ASN1(response.Data);

                                    foreach (ASN1 appTemplate in aefVal)
                                    {
                                        // Check we really have an Application Template
                                        if (appTemplate.Tag[0] == 0x61)
                                        {
                                            applicationIdentifiers.Add(appTemplate.Find(0x4f).Value);
                                        }
                                    }
                                } 
                                recordNumber++;
                            }
                        } 
                        if (pseFound)
                            break;
                    } 
                    cardReaderList.Disconnect();
                } 

                if (applicationIdentifiers.Count > 0)
                {
                    foreach (byte[] AID in applicationIdentifiers)
                    {
                        List<AppliFileLoc> applicationFileLocators = new List<AppliFileLoc>();
                        StringBuilder sb = new StringBuilder();
                        cardReaderList.Connect(selectedReader);

                        // Select AID
                        apdu = new APDUCmd(0x00, 0xA4, 0x04, 0x00, AID, (byte)AID.Length);
                        response = cardReaderList.Transmit(apdu);

                        // Get response nescesary
                        if (response.SW1 == 0x61)
                        {
                            apdu = new APDUCmd(0x00, 0xC0, 0x00, 0x00, null, response.SW2);
                            response = cardReaderList.Transmit(apdu);
                        }

                        // Application not found
                        if (response.SW1 == 0x6A && response.SW2 == 0x82)
                            continue;

                        if (response.SW1 == 0x90)
                        {
                            foreach (byte b in AID)
                            {
                                sb.AppendFormat("{0:X2}", b);
                            }

                            daTa = new ASN1(response.Data);

                            // Get processing options (with empty PDOL)
                            apdu = new APDUCmd(0x80, 0xA8, 0x00, 0x00, new byte[] { 0x83, 0x00 }, 0x02);
                            response = cardReaderList.Transmit(apdu);

                            // Get response nescesary
                            if (response.SW1 == 0x61)
                            {
                                apdu = new APDUCmd(0x00, 0xC0, 0x00, 0x00, null, response.SW2);
                                response = cardReaderList.Transmit(apdu);
                            }

                            if (response.SW1 == 0x90)
                            {
                                ASN1 template = new ASN1(response.Data);
                                ASN1 aipVal = null;
                                ASN1 aflVal = null;

                                // Primative response (Template Format 1)
                                if (template.Tag[0] == 0x80)
                                {
                                    byte[] tempAIP = new byte[2];
                                    Buffer.BlockCopy(template.Value, 0, tempAIP, 0, 2);
                                    aipVal = new ASN1(0x82, tempAIP);

                                    byte[] tempAFL = new byte[template.Length - 2];
                                    Buffer.BlockCopy(template.Value, 2, tempAFL, 0, template.Length - 2);
                                    aflVal = new ASN1(0x94, tempAFL);
                                }

                                // constructed data object response (Template Format 2)
                                if (template.Tag[0] == 0x77)
                                {
                                    aipVal = template.Find(0x82);
                                    aflVal = template.Find(0x94);
                                }

                                // Chop up AFL's
                                for (int i = 0; i < aflVal.Length; i += 4)
                                {
                                    byte[] AFL = new byte[4];
                                    Buffer.BlockCopy(aflVal.Value, i, AFL, 0, 4);

                                    AppliFileLoc fileLocator = new AppliFileLoc(AFL);
                                    applicationFileLocators.Add(fileLocator);
                                } 

                                ASN1 aipafl = new ASN1(response.Data);

                                foreach (AppliFileLoc file in applicationFileLocators)
                                {
                                    int rec = file.FirstRecord;// read SDA records 
                                    int lrec = file.LastRecord; 
                                    byte p2 = (byte)((file.SFI << 3) | 4); 

                                    while (rec <= lrec)
                                    {
                                        apdu = new APDUCmd(0x00, 0xB2, (byte)rec, p2, null, 0x00);
                                        response = cardReaderList.Transmit(apdu);

                                        // Retry with correct length
                                        if (response.SW1 == 0x6C)
                                        {
                                            apdu = new APDUCmd(0x00, 0xB2, (byte)rec, p2, null, response.SW2);
                                            response = cardReaderList.Transmit(apdu);
                                        }

                                        ASN1 record = new ASN1(response.Data);
                                        s_Card(record); 
                                        getVal(record); 
                                        
                                        rec++;
                                    }
                                } 
                            }
                            else
                            {
                                
                                UpdateStatusLabel(String.Format("Record not found."));
                            }
                        }
                        else
                        { 
                            UpdateStatusLabel(String.Format("File not selected."));
                        } 
                        cardReaderList.Disconnect();
                        UpdateStatusLabel(String.Format("Process Complete.")); 
                    }
                } 
            }
            catch (CardDExcep ex)
            {
                UpdateStatusLabel(ex.Message);
                return;
            } 
        } 

        private void getVal(ASN1 record)
        {
            string nm = "5F20";
            byte  mm = 0x2F;

            StringBuilder sb = new StringBuilder();
            foreach (ASN1 ad in record)
            { 
                foreach (byte b in ad.Tag)
                { 
                    sb.AppendFormat("{0:X2}", b);
                }
                string result = sb.ToString();

                if (nm == result)
                {
                    sb = new StringBuilder();
                    foreach (byte n in ad.Value)
                    {
                        if (n == mm)
                            break;
                        else
                        sb.AppendFormat("{0:X2}", n);
                       
                    }

                    string jk = sb.ToString();   
                    byte[] result1 = new byte[jk.Length / 2];

                    for (int i = 0; i < jk.Length; i += 2)
                    {
                        result1[i / 2] = byte.Parse(jk.Substring(i, 2), NumberStyles.HexNumber);
                    }
                    this.Invoke((MethodInvoker)delegate
                    {
                        lblCHNm.Text = encoding.GetString(result1);
                    }); 
                }
            }
        }


        private void s_Card(ASN1 record)
        {
            {
                foreach (ASN1 ad in record)
                {
                    byte nm = 0x5A;//, nn = 0x5f, h = 0x20;
                    if (ad.Tag[0] == nm)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (byte b in ad.Value)
                        {
                            sb.AppendFormat("{0:X2}", b);

                        }
                        string res = "" ,result = sb.ToString();
                               res = result.Insert(5, "*").Insert(6, "*").Insert(7,"*");
                         
                            this.Invoke((MethodInvoker)delegate
                            {
                                lblCardNo.Text = res;
                            });

                    }
                }
            } 
        }   
    } 
}