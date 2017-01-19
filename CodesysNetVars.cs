/*
 * Created by SharpDevelop.
 * User: Stefan Roßmann
 * Date: 10.05.2013
 * Time: 16:06
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace EasyNetVars
{
	/// <summary>
	/// Receive Broadcast Messages from CoDeSys Devices
	/// </summary>
	public class EasyNetVars
	{
		private int cobID=1;
		private int port=1202;
		private int numberOfTags=0;
		private List<CDataTypeCollection> dataType = new List<CDataTypeCollection>();
		private UdpClient udpClient;
		private string ipAddress = "190.201.100.100";
		private IPEndPoint iPEndPoint;
        private CTelegram cTelegramReceive;
        private ArrayList returnList = new ArrayList();
	
		public EasyNetVars()
		{
		}
		
        /// <summary>
        /// closes port used by UDP-Client
        /// </summary>			
		public void disconnect()
		{		
			if (udpClient != null)
			{
				udpClient.Close();
			}
		}

        /// <summary>
        /// create a new UDPClient. Method is not mandatory
        /// </summary>		
		public void connect()
		{		
			udpClient = new UdpClient(port);
			udpClient.Client.ReceiveTimeout = 5000;
			iPEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            
		}

        /// <summary>
        /// Reads values from CoDeSys PLC. Datatypes are listed in "dataTypeCollection".
        /// </summary>
        /// <returns>ArrayList which contains the values from CoDeSys PLC</returns>		
		public ArrayList ReadValues()
		{
            numberOfTags = dataType.Count;
			DateTime dt1 = DateTime.Now;
			DateTime dt2;
			byte[] receiveBytes = new byte[12];
			int elementCount;
			bool elementZeroDetected = false;
			bool waitForNextElement = true;
			int lastTelegramCount = 0;
			UInt16 subIndex = 1;
            returnList = new ArrayList();
            if (udpClient == null)
            	this.connect();
            while (((receiveBytes[8] != cobID)|receiveBytes==null|waitForNextElement|elementZeroDetected==false|returnList.Count!=dataType.Count))
			{
				dt2 = DateTime.Now;
				if ((dt2.Ticks -dt1.Ticks) > 100000000)
				{
					throw new Exception("Error receiving UDP-Messages; Check cob-ID, Port and IP-Address");
				}

				receiveBytes = udpClient.Receive(ref iPEndPoint);

				elementCount = 20;
                #region createTelegramInformation
                    cTelegramReceive = new CTelegram();
                    Buffer.BlockCopy(receiveBytes, 0, cTelegramReceive.Identity, 0, 4);
                    UInt32[] uintarray = new UInt32[1];
                    UInt16[] uint16array = new UInt16[1];
                    byte[] bytearray = new byte[1];

                    Buffer.BlockCopy(receiveBytes, 4, uintarray, 0, 4);
                    cTelegramReceive.ID = uintarray[0];
                    Buffer.BlockCopy(receiveBytes, 8, uint16array, 0, 2);
                    cTelegramReceive.Index = uint16array[0];
                    Buffer.BlockCopy(receiveBytes, 10, uint16array, 0, 2);
                    cTelegramReceive.SubIndex = uint16array[0];
                    subIndex =  cTelegramReceive.SubIndex;
                    Buffer.BlockCopy(receiveBytes, 12, uint16array, 0, 2);
                    cTelegramReceive.Items = uint16array[0];
                    Buffer.BlockCopy(receiveBytes, 14, uint16array, 0, 2);
                    cTelegramReceive.Length = uint16array[0];
                    Buffer.BlockCopy(receiveBytes, 16, uint16array, 0, 2);
                    cTelegramReceive.Counter = uint16array[0];
                    Buffer.BlockCopy(receiveBytes, 18, bytearray, 0, 1);
                    cTelegramReceive.Flags = bytearray[0];
                    Buffer.BlockCopy(receiveBytes, 19, bytearray, 0, 1);
                    cTelegramReceive.Checksum = bytearray[0];
                    cTelegramReceive.Data = new byte[receiveBytes.Length-20];
                    Buffer.BlockCopy(receiveBytes, 20, cTelegramReceive.Data, 0, cTelegramReceive.Data.Length);
                #endregion
                if ((cTelegramReceive.SubIndex == 0) & (receiveBytes[8] == cobID))
                	{
                		elementZeroDetected = true;
                		returnList.Clear();
                		lastTelegramCount = 0;
                	}
               
                if ((lastTelegramCount + 1) != (cTelegramReceive.SubIndex) &  lastTelegramCount != 0 & (receiveBytes[8] == cobID))
                {
               		elementZeroDetected = false;
                	returnList.Clear();
                }
                if (cobID == cTelegramReceive.Index)
					lastTelegramCount = cTelegramReceive.SubIndex;
                if ((lastTelegramCount) != (CountNumberOfDatagrams(returnList.Count)+1) &  lastTelegramCount != 0 & (receiveBytes[8] == cobID))
                {
               		elementZeroDetected = false;
                	returnList.Clear();
                }
                if (receiveBytes[8] == cobID & elementZeroDetected)
					{
					for (int i=0; i<cTelegramReceive.Items; i++)
					{
						
                        CDataTypeCollection cDataTypeCollection = (CDataTypeCollection)dataType[returnList.Count];
                        if (cDataTypeCollection.DataTypes == DataTypes.booltype)
						{
							bool boolValue = Convert.ToBoolean(receiveBytes[elementCount]);
							returnList.Add(boolValue);
							elementCount = elementCount + 1;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.bytetype)
						{
							byte byteValue = receiveBytes[elementCount];
							returnList.Add(byteValue);
							elementCount = elementCount + 1;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.wordtype)
						{
							UInt16 wordValue = Convert.ToUInt16(receiveBytes[elementCount]
						 	                                   |(receiveBytes[elementCount+1] << 8));
							returnList.Add(wordValue);
							elementCount = elementCount + 2;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.dwordtype)
						{
							UInt32 dwordValue = ((UInt32)receiveBytes[elementCount]
							                     |((UInt32)receiveBytes[elementCount+1] << 8)
							                     |((UInt32)receiveBytes[elementCount+2] << 16)
							                     |((UInt32)receiveBytes[elementCount+3] << 24));
							returnList.Add(dwordValue);
							elementCount = elementCount + 4;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.sinttype)
						{
							sbyte sintValue = (sbyte)receiveBytes[elementCount];
							returnList.Add(sintValue);
							elementCount = elementCount + 1;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.usintType)
						{
							byte usintValue = receiveBytes[elementCount];
							returnList.Add(usintValue);
							elementCount = elementCount + 1;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.inttype)
						{
							Int16 intValue = BitConverter.ToInt16(receiveBytes,elementCount);
							returnList.Add(intValue);
							elementCount = elementCount + 2;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.uinttype)
						{
							UInt16 usintValue = Convert.ToUInt16(receiveBytes[elementCount]
							                                    |(receiveBytes[elementCount+1] << 8));
							returnList.Add(usintValue);
							elementCount = elementCount + 2;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.udinttype)
						{
							UInt32 udintValue = ((UInt32)receiveBytes[elementCount]
							                     |((UInt32)receiveBytes[elementCount+1] << 8)
							                     |((UInt32)receiveBytes[elementCount+2] << 16)
							                     |((UInt32)receiveBytes[elementCount+3] << 24));
							returnList.Add(udintValue);
							elementCount = elementCount + 4;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.dinttype)
						{
							Int32 dintValue = BitConverter.ToInt32(receiveBytes,elementCount);
							returnList.Add(dintValue);
							elementCount = elementCount + 4;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.realtype)
						{
							float floatValue = BitConverter.ToSingle(receiveBytes,elementCount);
							returnList.Add(floatValue);
							elementCount = elementCount + 4;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.lrealtype)
						{
							double dfloatValue = BitConverter.ToDouble(receiveBytes,elementCount);
							returnList.Add(dfloatValue);
							elementCount = elementCount + 8;
						}
                        if (cDataTypeCollection.DataTypes == DataTypes.stringtype)
                        {                         
                            string stringValue = System.Text.Encoding.UTF8.GetString(receiveBytes,elementCount,cDataTypeCollection.FieldLength);
                            int nullSignPosition = stringValue.Length;
                            for (int j = 0; j < stringValue.Length; j++)
                            {
                                if (receiveBytes[elementCount + j] == 0)
                                {
                                    nullSignPosition = j;
                                    break;
                                }
                            }
                            stringValue = stringValue.Substring(0, nullSignPosition);
                            returnList.Add(stringValue);
                            elementCount = elementCount + cDataTypeCollection.FieldLength + 1;
                        }                        
					}
				}
                
                if (((this.CountNumberOfDatagrams(dataType.Count)) <= subIndex) & elementZeroDetected & receiveBytes[8] == cobID)
                	waitForNextElement=false;
			}
			return returnList;
		}
		
        /// <summary>
        /// Calculate number of datagrams necessary for the number of tags which are defined by "index". Datagrams will be 
        /// devided if datagram size is more than 256 byte
        /// </summary>
        /// <param name="index">Tag number for calculation of datagrams</param>    
        /// <returns>Number of datagrams necessary</returns>   
		private int CountNumberOfDatagrams(int index)
		{
			int countValue = 0;
			int valueBefore = 0;
			int valueAfter = 0;
			int returnValue;
			for (int i=0; (i<dataType.Count & i<index); i++)
			{
				valueBefore = countValue;
				//1 Byte Breite Datentypen
				if ((dataType[i].DataTypes == DataTypes.booltype)
				    | (dataType[i].DataTypes == DataTypes.bytetype)
				    | (dataType[i].DataTypes == DataTypes.sinttype)
				    | (dataType[i].DataTypes == DataTypes.usintType))
				{
					countValue = countValue + 1;
					valueAfter = countValue;

						
				}
				//2 Byte Breite Datentypen
				if ((dataType[i].DataTypes == DataTypes.wordtype)
				    | (dataType[i].DataTypes == DataTypes.inttype)
				    | (dataType[i].DataTypes == DataTypes.uinttype))
				{
					countValue = countValue + 2;
					valueAfter = countValue;
					if (valueAfter%256 <= valueBefore%256 & valueAfter%256 != 0)
					{
						countValue = (int)(256*Math.Ceiling((float)valueBefore/256)) + 2;
					}
				}
				//4 Byte Breite Datentypen
				if ((dataType[i].DataTypes == DataTypes.dwordtype)
				    | (dataType[i].DataTypes == DataTypes.udinttype)
				    | (dataType[i].DataTypes == DataTypes.dinttype)
				    | (dataType[i].DataTypes == DataTypes.realtype))
				{
					countValue = countValue + 4;
					valueAfter = countValue;
					if (valueAfter%256 <= valueBefore%256 & valueAfter%256 != 0)
					{
						countValue = (int)(256*Math.Ceiling((float)valueBefore/256)) + 4;
					}
				}
				//8 Byte Breite Datentypen
				if ((dataType[i].DataTypes == DataTypes.lrealtype))
				{
					countValue = countValue + 8;
					valueAfter = countValue;
					if (valueAfter%256 <= valueBefore%256 & valueAfter%256 != 0)
					{
						countValue = (int)(256*Math.Ceiling((float)valueBefore/256)) + 8;
					}
				}
				//Stringdatentyp
				if ((dataType[i].DataTypes == DataTypes.stringtype))
				{
					countValue = countValue + dataType[i].FieldLength + 1;
					valueAfter = countValue;
					if (valueAfter%256 <= valueBefore%256 & valueAfter%256 != 0)
					{
						countValue = (int)(256*Math.Ceiling((float)valueBefore/256)) + dataType[i].FieldLength + 1;
					}
				}
			}
			
			returnValue = (int)(Math.Floor((float)countValue/256));
            if ((countValue % 256) == 0 & returnValue > 0)
                returnValue--;
			return returnValue;
		}
			

        private ushort sendCounter = 0;
        public void SendValues()
        {
        	Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);     	
        	s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        	int numberOfDatagrams = this.CountNumberOfDatagrams(dataType.Count)+1;
        	int numberOfTagsInDatagram=0;
        	int datagramCounter=0;
        	int tagCounter = 0;
            numberOfTags = dataType.Count;
            byte[] send_buffer;
            CTelegram cTelegramSend = new CTelegram();
            IPAddress broadcast = IPAddress.Parse(ipAddress);
            IPEndPoint endPoint = new IPEndPoint(broadcast, port);
            s.EnableBroadcast = true;           
            for (int k = 0; k < numberOfDatagrams; k++)
            {
            	if (sendCounter >= 65535)
            		sendCounter = 0;
            	else
            		sendCounter++;
            numberOfTagsInDatagram = 0;
            tagCounter++;
            numberOfTagsInDatagram++;
            while ((this.CountNumberOfDatagrams(tagCounter) == datagramCounter) & (tagCounter != dataType.Count))
            {
            	numberOfTagsInDatagram++;
                tagCounter++;

            }
            if (this.CountNumberOfDatagrams(tagCounter) > datagramCounter)
            {
                tagCounter--;
                numberOfTagsInDatagram--;
                
            }
            #region createTelegramInformatiom
            	send_buffer = new byte[20];
                UInt32[] uintarray = new UInt32[1];
                Int32[] intarray = new Int32[1];
                UInt16[] uint16array = new UInt16[1];
                Int16[] int16array = new Int16[1];
                byte[] bytearray = new byte[1];
                sbyte[] sbytearray = new sbyte[1];
                float[] floatarray = new float[1];
                double[] doublearray = new double[1];
                Buffer.BlockCopy(cTelegramSend.Identity, 0, send_buffer, 0, 4);
                uintarray[0] = cTelegramSend.ID;
                Buffer.BlockCopy(uintarray, 0, send_buffer, 4, 4);
                cTelegramSend.Index = (ushort)cobID;
                uint16array[0] = cTelegramSend.Index;
                Buffer.BlockCopy(uint16array, 0, send_buffer, 8, 2);
                cTelegramSend.SubIndex = (ushort)datagramCounter;
                uint16array[0] = cTelegramSend.SubIndex;
                Buffer.BlockCopy(uint16array, 0, send_buffer, 10, 2);
                cTelegramSend.Items = (ushort)numberOfTagsInDatagram;
                uint16array[0] = cTelegramSend.Items;
                Buffer.BlockCopy(uint16array, 0, send_buffer, 12, 2);
               
                cTelegramSend.Counter = (ushort)sendCounter;
                uint16array[0] = cTelegramSend.Counter;
                Buffer.BlockCopy(uint16array, 0, send_buffer, 16, 2);
                send_buffer[18] = cTelegramSend.Flags;
                send_buffer[19] = cTelegramSend.Checksum;
                int byteCount = 0;
                for (int i = 0; i < numberOfTagsInDatagram; i++)
                {
                    switch (dataType[i + this.CountNumberOfDatagrams(tagCounter)].DataTypes)
                    {
                        case DataTypes.booltype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 1);
                            send_buffer[20 + byteCount] = Convert.ToByte(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            byteCount++;
                            break;
                        case DataTypes.bytetype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 1);
                            send_buffer[20 + byteCount] = Convert.ToByte(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            byteCount++;
                            break;
                        case DataTypes.wordtype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 2);
                            uint16array[0] = Convert.ToUInt16(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(uint16array, 0, send_buffer, 20+byteCount, 2);
                            byteCount = byteCount + 2;
                            break;
                        case DataTypes.dwordtype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 4);
                            uintarray[0] = Convert.ToUInt32(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(uintarray, 0, send_buffer, 20 + byteCount, 4);
                            byteCount = byteCount + 4;
                            break;
                        case DataTypes.sinttype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 1);
                            sbytearray[0] = Convert.ToSByte(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(sbytearray, 0, send_buffer, 20 + byteCount, 1);
                            byteCount = byteCount + 1;
                            break;
                        case DataTypes.usintType:
                            Array.Resize(ref send_buffer, send_buffer.Length + 1);
                            send_buffer[20 + byteCount] = Convert.ToByte(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            byteCount++;
                            break;
                        case DataTypes.inttype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 2);
                            int16array[0] = Convert.ToInt16(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(int16array, 0, send_buffer, 20 + byteCount, 2);
                            byteCount = byteCount + 2;
                            break;
                        case DataTypes.uinttype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 2);
                            uint16array[0] = Convert.ToUInt16(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(uint16array, 0, send_buffer, 20 + byteCount, 2);
                            byteCount = byteCount + 2;
                            break;
                        case DataTypes.dinttype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 4);
                            intarray[0] = Convert.ToInt32(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(intarray, 0, send_buffer, 20 + byteCount, 4);
                            byteCount = byteCount + 4;
                            break;
                        case DataTypes.udinttype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 4);
                            uintarray[0] = Convert.ToUInt32(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(uintarray, 0, send_buffer, 20 + byteCount, 4);
                            byteCount = byteCount + 4;
                            break;
                        case DataTypes.realtype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 4);
                            floatarray[0] = (float)Convert.ToDouble(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(floatarray, 0, send_buffer, 20 + byteCount, 4);
                            byteCount = byteCount + 4;
                            break;
                        case DataTypes.lrealtype:
                            Array.Resize(ref send_buffer, send_buffer.Length + 8);
                            doublearray[0] = Convert.ToDouble(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue);
                            Buffer.BlockCopy(doublearray, 0, send_buffer, 20 + byteCount, 8);
                            byteCount = byteCount + 8;
                            break;
                        case DataTypes.stringtype:
                            char[] charArray = new char[dataType[i + this.CountNumberOfDatagrams(tagCounter)].FieldLength + 1];
                            byte[] byteArray = new byte[dataType[i + this.CountNumberOfDatagrams(tagCounter)].FieldLength + 1];
                            charArray = Convert.ToString(dataType[i + this.CountNumberOfDatagrams(tagCounter)].SendValue).ToCharArray();
                            int oldLength = charArray.Length;

                            if (charArray.Length <= dataType[i + this.CountNumberOfDatagrams(tagCounter)].FieldLength)
                                Array.Resize(ref charArray, (dataType[i + this.CountNumberOfDatagrams(tagCounter)].FieldLength + 1));
                            charArray[oldLength] = (char)0x0;
                            Array.Resize(ref send_buffer, send_buffer.Length + charArray.Length);
                            for (int j = 0; j < charArray.Length; j++)
                            {
                                byteArray[j] = (byte)charArray[j];
                            }
                            Buffer.BlockCopy(byteArray, 0, send_buffer, 20 + byteCount, (byteArray.Length));
                            byteCount = byteCount + charArray.Length;
                            break;
                        default: 
                            break;
                    }
                }
                cTelegramSend.Length = (ushort)send_buffer.Length;
                uint16array[0] = cTelegramSend.Length;
                Buffer.BlockCopy(uint16array, 0, send_buffer, 14, 2);
            #endregion

                s.SendTo(send_buffer, endPoint);
                datagramCounter++;
            }
        }

        public void CreateGVLFile(string fileName)
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlNode xmlRoot, xmlNode, xmlNode2;
            XmlAttribute xmlAttribute;
            xmlRoot = xmlDocument.CreateElement("GVL");
            xmlDocument.AppendChild(xmlRoot);

            xmlNode = xmlDocument.CreateElement("Declarations");
#region CreateCDataSection
            string cDataSection = "VAR_GLOBAL";
            for (int i = 0; i < dataType.Count; i++)
            {
                if (dataType[i].VariableName != null)
                    cDataSection = cDataSection + '\u000A' + '\u0009' + dataType[i].VariableName;
                else
                    cDataSection = cDataSection + '\u000A' + '\u0009' + "variable" + i.ToString();
                switch (dataType[i].DataTypes)
                {
                    case DataTypes.booltype:
                        cDataSection = cDataSection + ": BOOL;";
                        break;
                    case DataTypes.bytetype:
                        cDataSection = cDataSection + ": BYTE;";
                        break;
                    case DataTypes.wordtype:
                        cDataSection = cDataSection + ": WORD;";
                        break;
                    case DataTypes.dwordtype:
                        cDataSection = cDataSection + ": DWORD;";
                        break;
                    case DataTypes.sinttype:
                        cDataSection = cDataSection + ": SINT;";
                        break;
                    case DataTypes.usintType:
                        cDataSection = cDataSection + ": USINT;";
                        break;
                    case DataTypes.inttype:
                        cDataSection = cDataSection + ": INT;";
                        break;
                    case DataTypes.uinttype:
                        cDataSection = cDataSection + ": UINT;";
                        break;
                    case DataTypes.dinttype:
                        cDataSection = cDataSection + ": DINT;";
                        break;
                    case DataTypes.udinttype:
                        cDataSection = cDataSection + ": UDINT;";
                        break;
                    case DataTypes.realtype:
                        cDataSection = cDataSection + ": REAL;";
                        break;
                    case DataTypes.lrealtype:
                        cDataSection = cDataSection + ": LREAL;";
                        break;
                    case DataTypes.stringtype:
                        cDataSection = cDataSection + ": STRING("+dataType[i].FieldLength+");";
                        break;
                }
            }
            cDataSection = cDataSection + '\u000A' + "END_VAR";
#endregion

            xmlNode2 = xmlDocument.CreateCDataSection(cDataSection);
            xmlNode.AppendChild(xmlNode2);
            xmlRoot.AppendChild(xmlNode);

            xmlNode = xmlDocument.CreateElement("NetvarSettings");
            xmlAttribute = xmlDocument.CreateAttribute("Protocol");
            xmlAttribute.Value = "UDP";
            xmlNode.Attributes.Append(xmlAttribute);
            xmlNode2 = xmlDocument.CreateElement("ListIdentifier");
            xmlNode2.InnerText = cobID.ToString();
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("Pack");
            xmlNode2.InnerText = "TRUE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("Checksum");
            xmlNode2.InnerText = "FALSE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("Acknowledge");
            xmlNode2.InnerText = "FALSE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("CyclicTransmission");
            xmlNode2.InnerText = "TRUE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("TransmissionOnChange");
            xmlNode2.InnerText = "FALSE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("TransmissionOnEvent");
            xmlNode2.InnerText = "FALSE";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("Interval");
            xmlNode2.InnerText = "T#50ms";
            xmlNode.AppendChild(xmlNode2);
            xmlNode2 = xmlDocument.CreateElement("MinGap");
            xmlNode2.InnerText = "T#20ms";
            xmlNode.AppendChild(xmlNode2);
            xmlRoot.AppendChild(xmlNode);
            xmlDocument.Save(fileName);
        }

		
		public static DataTypes ConvertIntToDataTypes(int dataTypes)
		{
			switch (dataTypes)
			{
				case 1: return DataTypes.booltype;
					
				case 2: return DataTypes.bytetype;
					
				case 3: return DataTypes.wordtype;
					
				case 4: return DataTypes.dwordtype;
					
				case 5:	return DataTypes.sinttype;
					
				case 6:	return DataTypes.usintType;
					
				case 7:	return DataTypes.inttype;
					
				case 8:	return DataTypes.uinttype;
					
				case 9:	return DataTypes.dinttype;
					
				case 10:return DataTypes.udinttype;
					
				case 11:return DataTypes.realtype;
					
				case 12:return DataTypes.lrealtype;

                case 13: return DataTypes.stringtype;
					
				default: return DataTypes.booltype;
			}
		}
		
		/// <summary>
		/// Network identifier of CoDeSys Network Variablelist
		/// </summary>
		public int CobID
		{
			get
			{
				return cobID;
			}
			set
			{
				cobID = value;
			}
		}
		
		/// <summary>
		/// Port for Dataexchange
		/// </summary>
		public int Port
		{
			get
			{
				return port;
			}
			set
			{
				port = value;
			}
		}
		
		/// <summary>
		/// IP-Adress for Send-Operation
		/// </summary>
		public string IPAdress
		{
			get
			{
				return ipAddress;
			}
			set
			{
				ipAddress = value;
			}
		}

		/// <summary>
		/// Listed Datatypes which corresponds to the Network variableList
		/// </summary>
        public List<CDataTypeCollection> dataTypeCollection
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
                numberOfTags = dataTypeCollection.Count;
            }
        }

        /// <summary>
		/// Telegram information
		/// </summary>
        public CTelegram CTelegramReceive
        {
            get
            {
                return cTelegramReceive;
            }
        }

		/// <summary>
		/// number of tags in ArrayList
		/// </summary>
        public int NumberOfTags
        {
            get
            {
                return numberOfTags;
            }
            set
            {
                numberOfTags = value;
               
            }
        }
	}


    	public enum DataTypes:int
		{
			booltype = 1,
			bytetype = 2,
			wordtype = 3,
			dwordtype = 4,
			sinttype = 5,
			usintType = 6,
			inttype = 7,
			uinttype = 8,
			dinttype = 9,
			udinttype = 10,
			realtype = 11,
			lrealtype = 12,
            stringtype = 13
		}

        /// <summary>
        /// Datatype information consisting of Datatype and fieldlength (only necessary for Strings)
        /// </summary>
    public class CDataTypeCollection
    {
        private DataTypes dataTypes;
        private int fieldLength = 80;
        private object sendValue;
        private string variableName;

        public CDataTypeCollection()
		{
		}

        /// <summary>
        /// Constructor - defines the datatype
        /// </summary>
        /// <param name="dataTypes">Datatype information e.g. DataTypes.inttype</param>
        public CDataTypeCollection(DataTypes dataTypes)
        {
            this.dataTypes = dataTypes;
        }

        /// <summary>
        /// Constructor - defines the datatype and the field length (needed for Strings)
        /// </summary>
        /// <param name="dataTypes">Datatype information e.g. DataTypes.inttype</param>
        /// <param name="fieldLength">Length of a String</param>
        public CDataTypeCollection(DataTypes dataTypes, int fieldLength)
        {
            this.dataTypes = dataTypes;
            this.fieldLength = fieldLength;
        }

        /// <summary>
        /// Constructor - defines the datatype and the field length (needed for Strings) and value
        /// </summary>
        /// <param name="dataTypes">Datatype information e.g. DataTypes.inttype</param>
        /// <param name="fieldLength">Length of a String</param>
        /// <param name="value">Value to send</param>
        public CDataTypeCollection(object value, DataTypes dataTypes, int fieldLength)
        {
            this.dataTypes = dataTypes;
            this.fieldLength = fieldLength;
            this.sendValue = value;
        }

        /// <summary>
        /// Constructor - defines the datatype and value (for send)
        /// </summary>
        /// <param name="dataTypes">Datatype information e.g. DataTypes.inttype</param>
        /// <param name="value">Value to send</param>
        public CDataTypeCollection(object value, DataTypes dataTypes)
        {
            this.dataTypes = dataTypes;
            this.sendValue = value;
        }

        public DataTypes DataTypes
        {
            get
            {
                return dataTypes;
            }
            set
            {
                dataTypes = value;
            }
        }

        public int FieldLength
        {
            get
            {
                return fieldLength;
            }
            set
            {
                fieldLength = value;
            }
        }

        public object SendValue
        {
            get
            {
                return sendValue;
            }
            set
            {
                sendValue = value;
            }
        }

        public string VariableName
        {
            get
            {
                return variableName;
            }
            set
            {
                variableName = value;
            }
        }

    }

    public class CTelegram
    {
        /// <summary>
        /// CoDeSys protocol identity code "3S-0" (Byte0="0"; Byte1="-"; Byte2="S"; Byte3="3")
        /// </summary>
        public byte[] Identity= new byte[4] {0, 45, 83, 51};

        /// <summary>
        /// ID for Network Variables "0"
        /// </summary>
        public UInt32 ID = 0;

        /// <summary>
        /// Cob-ID
        /// </summary>
        public UInt16 Index = 1;       

        /// <summary>
        /// If "Pack-Variables" is disabled - Message ID
        /// </summary>
        public UInt16 SubIndex;    

        /// <summary>
        /// Number of Variables
        /// </summary>
        public UInt16 Items;       

        /// <summary>
        /// Total Size of the Message incl. Header
        /// </summary>
        public UInt16 Length;      

        /// <summary>
        /// Counts the number of sent telegrams
        /// </summary>
        public UInt16 Counter;    

        /// <summary>
        /// Bit0: Send-acknowledgement desired; Bit1: Check of checksum desired; Bit2: Invalid checksum
        /// </summary>
        public byte Flags;        

        /// <summary>
        /// Checksum of the datagram
        /// </summary>
        public byte Checksum;      

        /// <summary>
        /// Data
        /// </summary>
        public byte[] Data;     
    }
}
