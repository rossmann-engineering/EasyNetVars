/*
 * Created by SharpDevelop.
 * User: Stefan Roßmann
 * Date: 10.05.2013
 * Time: 16:05
 */
using System;
using System.Collections;


namespace EasyNetVars
{
	class Program
	{

		public static void Main(string[] args)
		{
            
            NetVars srCodesysWriteVars = null;
            NetVars srCodesysNetVars = null;

            srCodesysWriteVars = new NetVars();
            srCodesysWriteVars.IPAdress = "192.168.178.34";
            srCodesysWriteVars.CobID = 2;
            CDataTypeCollection collect;
            //srCodesysWriteVars.Port = 1203;

            collect = new CDataTypeCollection(-35, EasyNetVars.DataTypes.inttype);
            collect.VariableName = "rcvInt";
            srCodesysWriteVars.dataTypeCollection.Add(collect);

            collect = new CDataTypeCollection(0.123, EasyNetVars.DataTypes.realtype);
            collect.VariableName = "rcvReal";
            srCodesysWriteVars.dataTypeCollection.Add(collect);

            collect = new CDataTypeCollection(0x1010, EasyNetVars.DataTypes.wordtype);
            collect.VariableName = "rcvWord";
            srCodesysWriteVars.dataTypeCollection.Add(collect);

            collect = new CDataTypeCollection(true, EasyNetVars.DataTypes.booltype);
            collect.VariableName = "rcvBool";
            srCodesysWriteVars.dataTypeCollection.Add(collect);

            for (int i = 0; i < 300; i++)
            {
                collect = new CDataTypeCollection(123, EasyNetVars.DataTypes.bytetype);
                srCodesysWriteVars.dataTypeCollection.Add(collect);
            }

            //---nur für Konfiguration SPS erforderlich?    
            srCodesysWriteVars.CreateGVLFile("c:\\GVLFile.GVL");
            srCodesysWriteVars.SendValues();

            srCodesysNetVars = new NetVars();
            srCodesysNetVars.Port = 1204;
            srCodesysNetVars.IPAdress = "192.168.178.34";
           
            for (int i = 0; i < 514; i++)
                srCodesysNetVars.dataTypeCollection.Add(new CDataTypeCollection(EasyNetVars.DataTypes.bytetype));
            ArrayList ReceiveVar = srCodesysNetVars.ReadValues();
            Console.WriteLine(ReceiveVar[513].ToString());
            Console.ReadKey();
		}
	}
}