﻿using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using serialPortStd.Methods;
using serialPortStd.Models;

namespace serialPortStd
{
    class Program
    {
        public static int count = 0;
        public static List<byte> buff = new List<byte>();

        static void Main(string[] args)
        {
            string name = Helper.GetName();

            SerialPort port = new SerialPort();
            port.PortName = name;
            port.Parity = Parity.None;
            port.BaudRate = 230400;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.None;
            port.Encoding = Encoding.Default;

            if (port.IsOpen)
            {
                port.Close();
                port.Dispose();
            }
            try
            {
                port.Open();
                if (port.IsOpen)
                {
                    Console.WriteLine($"Port {name} is open.");
                }
                port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler2);

                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void DataReceivedHandler2(object sender, SerialDataReceivedEventArgs e)
        {
            if (sender != null)
            {
                SerialPort port = (SerialPort)sender;

                try
                {
                    while (port.IsOpen && port.BytesToRead > 0)
                    {
                        byte data = (byte)port.ReadByte();
                        var hex = data.ToString("X2");
                        if (data.ToString("X2").Contains("AA"))
                        {
                            if (buff.ToArray().Length >= 0 && buff.ToArray().Length > 11)
                            {
                                buff.ForEach(x => Console.Write(x.ToString("X2")));
                                var buffArray = buff.ToArray();
                                
                                #region checkSum
                                if (!Helper.GetControlSum(buffArray))
                                {
                                    Console.WriteLine(" << Check sume failure\n");

                                }
                                else
                                {
                                    var hexFrame = new HexFrameModel();
                                    hexFrame.FrameHeader = buffArray[0].ToString("X2");
                                    var frameLength = BitConverter.ToString(buffArray[1..3]).Replace("-", string.Empty);
                                    hexFrame.FrameLength = UInt16.Parse(frameLength, System.Globalization.NumberStyles.HexNumber);
                                    hexFrame.ProtocolVersion = buffArray[4].ToString("X2");
                                    hexFrame.FrameType = buffArray[5].ToString("X2");
                                    hexFrame.CommandWord = buffArray[6].ToString("X2");

                                    var hexEffectiveDataLength = BitConverter.ToString(buffArray[6..8]).Replace("-", string.Empty);
                                    var EffectiveDataLength = UInt16.Parse(hexEffectiveDataLength, System.Globalization.NumberStyles.HexNumber);
                                    hexFrame.EffectiveDataLength = (EffectiveDataLength - 5) / 3;

                                    var RadarSpeed = UInt16.Parse(buffArray[8].ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                                    hexFrame.RadarSpeed = RadarSpeed * 0.05;

                                    hexFrame.ZeroOffset = BitConverter.ToString(buffArray[9..11]).Replace("-", string.Empty);

                                    var StartingAngleHex = BitConverter.ToString(buffArray[11..13]).Replace("-", string.Empty);
                                    var StartingAngle = UInt16.Parse(StartingAngleHex, System.Globalization.NumberStyles.HexNumber);
                                    hexFrame.StartingAngle = StartingAngle * 0.01;

                                    var package = hexFrame.StartingAngle / 22.5;
                                    hexFrame.FrameHeader = hexFrame.FrameHeader + "-" + package;

                                    var pointList = new List<FramePoint>();
                                    double anglePoint = (22.5 / hexFrame.EffectiveDataLength);
                                    int first = 14;
                                    int last = 16;
                                    for (int i = 1; i <= hexFrame.EffectiveDataLength; i++)
                                    {
                                        var hexPoint = BitConverter.ToString(buffArray[first..last]).Replace("-", string.Empty);
                                        var uintPoint = UInt16.Parse(hexPoint, System.Globalization.NumberStyles.HexNumber);
                                        double point = uintPoint * 0.25;
                                        pointList.Add(
                                          new FramePoint()
                                          {
                                              point = i,
                                              anglePoint = (anglePoint * i) + hexFrame.StartingAngle,
                                              pointValue = point
                                          }
                                        );
                                        first += 3;
                                        last += 3;
                                    }
                                    hexFrame.framePoint = pointList;

                                    Console.WriteLine("\n-------------------------------------------");
                                    Console.WriteLine(" Frame header " + hexFrame.FrameHeader);
                                    Console.WriteLine(" Frame lenght " + hexFrame.FrameLength);
                                    Console.WriteLine(" effective Data length " + hexFrame.EffectiveDataLength);
                                    Console.WriteLine(" Radar Speed " + hexFrame.RadarSpeed);
                                    Console.WriteLine(" Starting Angle " + hexFrame.StartingAngle);
                                    Console.WriteLine(" Points in the frame:");
                                    hexFrame.framePoint.ForEach(x => Console.WriteLine($" Point [{x.point}] angle({x.anglePoint}°)- {x.pointValue} mm "));
                                    Console.WriteLine("-------------------------------------------\n");
                                }
                                #endregion

                                #region setNewPack
                                count = 1;
                                buff = new List<byte>();
                                #endregion
                            }
                            else
                            {
                                Console.WriteLine("Radar speed failure");
                            }

                        }
                        if (count > 0)
                        {
                            buff.Add(data);
                        }
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

            }
        }


        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;

            UInt16 crcCheckNum = 0, calcchecksum;

            var pointList = new List<FramePoint>();
            int bytes = port.BytesToRead;
            byte[] buffer = new byte[bytes];


            if (bytes > 1)
            {
                port.Read(buffer, 0, bytes);
                Console.WriteLine();
                Console.WriteLine(BitConverter.ToString(buffer));
                Console.WriteLine();
                #region test
                //unsafe
                //{
                //    UInt16 frameLens = (ushort)((buffer[1] << 8) | buffer[2]);

                //    fixed (byte* pointerToFirst = &buffer[0])
                //    {
                //        crcCheckNum = (ushort)(pointerToFirst + frameLens);
                //        var angleRange = (buffer[11] << 8) | buffer[12];


                //        if (buffer[3] > 0)
                //            calcchecksum = Helper._checksum(pointerToFirst, frameLens);
                //        else
                //            calcchecksum = Helper.Crc16(pointerToFirst, frameLens);
                //        if (crcCheckNum == calcchecksum)
                //        {

                //        }
                //        Console.WriteLine(crcCheckNum);
                //        Console.WriteLine(calcchecksum);
                //    }
                //}
                #endregion
                string hexBuff = "";

                //foreach (var item in buffer)
                //{
                //    count++;

                //    var hex = item.ToString("X2");
                //    if (hex.Contains("AA"))
                //    {
                //        count = 1;
                //        hexFrame = new HexFrameModel();
                //        circle++;
                //        if (circle > 16) { circle = 1; }
                //        hexFrame.FrameHeader = hex + "-" + circle;
                //        Console.WriteLine();
                //        Console.WriteLine("Data receive at time " + DateTime.Now.TimeOfDay + " circle: " + ((circle / 16) * 100) + " %");
                //    }
                //    #region test2
                //    //if (count == 2 || count == 3)
                //    //{
                //    //    hexBuff += hex;
                //    //    if (count == 3)
                //    //    {
                //    //        var uIntValue = UInt16.Parse(hexBuff, System.Globalization.NumberStyles.HexNumber);
                //    //        hexFrame.FrameLength += uIntValue;
                //    //        hexBuff = "";
                //    //    }
                //    //}

                //    //if (count == 4) { hexFrame.ProtocolVersion = hex; }
                //    //if (count == 5) { hexFrame.FrameType = hex; }
                //    //if (count == 6) { hexFrame.CommandWord = hex; }
                //    //if (count == 7 || count == 8)
                //    //{
                //    //    hexBuff += hex;
                //    //    if (count == 8)
                //    //    {
                //    //        var uIntValue = UInt16.Parse(hexBuff, System.Globalization.NumberStyles.HexNumber);
                //    //        hexFrame.EffectiveDataLength = (uIntValue - 5) / 3;
                //    //        hexBuff = "";
                //    //    }
                //    //}
                //    //if (count == 9)
                //    //{
                //    //    var uIntValue = UInt16.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                //    //    hexFrame.RadarSpeed = uIntValue * 0.05;
                //    //}
                //    //if (count == 10 || count == 11)
                //    //{
                //    //    hexBuff += hex;
                //    //    if (count == 11)
                //    //    {
                //    //        hexFrame.ZeroOffset = hexBuff;
                //    //        hexBuff = "";
                //    //    }
                //    //}
                //    //if (count == 12 || count == 13)
                //    //{
                //    //    hexBuff += hex;
                //    //    if (count == 13)
                //    //    {
                //    //        var uIntValue = UInt16.Parse(hexBuff, System.Globalization.NumberStyles.HexNumber);
                //    //        hexFrame.StartingAngle = uIntValue * 0.01;
                //    //        hexBuff = "";
                //    //    }
                //    //}
                //    //if (count >= 14 && count <= hexFrame.EffectiveDataLength)
                //    //{
                //    //    for (int i = 1; i < hexFrame.EffectiveDataLength; i++)
                //    //    {
                //    //        hexFrame.framePoint = pointList.Add(
                //    //          new FramePoint { signal = i, signalVale = "" }
                //    //        );
                //    //    }
                //    //}
                //    #endregion
                //}
            }

            //if (hexFrame != null && !hexFrame.IsEmpty())
            //{
            //    Console.WriteLine();
            //    Console.WriteLine(" Frame header " + hexFrame.FrameHeader);
            //    Console.WriteLine(" Frame lenght " + hexFrame.FrameLength);
            //    Console.WriteLine(" effective Data length " + hexFrame.EffectiveDataLength);
            //    Console.WriteLine(" Radar Speed " + hexFrame.RadarSpeed);
            //    Console.WriteLine(" Starting Angle " + hexFrame.StartingAngle);
            //    Console.WriteLine();
            //}
        }
    }
}
