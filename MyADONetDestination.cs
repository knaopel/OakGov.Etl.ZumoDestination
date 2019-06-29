using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.SqlServer.Dts.ManagedMsg;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Localization;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.Win32;
using System.Globalization;
using System.Collections;
using System.Configuration;
using System.Data.OleDb;
using System.Text.RegularExpressions;

namespace OakGov.Etl.ZumoDestination
{
    [ComVisible(false)]
    [DtsPipelineComponent(DisplayName="MyADO NET Destination",ComponentType=ComponentType.DestinationAdapter,IconResource="OakGov.Etl.ZumoDestination.Microsoft.SqlServer.DtsPipeline.ADONETDest.ico",UITypeName="Microsoft.DataTransorfmationServices.DataFlowUI.ADONETDestinationUI,Microsoft.DatatransformationServices.DataFlowUI, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91", LocalizationType=typeof(OakGov.Etl.ZumoDestination.Localized),CurrentVersion=2,HelpKeyword="sql11.dts.design.adonetdest.f1",SamplesTag="SsisAdoNetDestination",RequiredProductLevel=Microsoft.SqlServer.Dts.Runtime.Wrapper.DTSProductLevel.DTSPL_NONE)]
    public class MyADONetDestination : PipelineComponent
    {
        private const string RUNTIME_CONN_NAME = "IDbConnection";

        private const string TABLE_OR_VIEW_NAME = "TableOrViewName";

        private const string BATCH_SIZE = "BatchSize";

        private const string COMMAND_TIMEOUT = "CommandTimeout";

        private const string USE_BULKINSERTWHENPOSSIBLE = "UseBulkInsertWhenPossible";

        private const string SKIPINSERT_MODE = "SkipInsertMode";

        private const string STRINGEDITOR = "Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor, Microsoft.DataTransformationServices.Controls, Version= {0}, Culture=neutral, PublicKeyToken=89845dcd8080cc91";

        private const string COLUMNNAME = "ColumnName";

        private const string DATATYPE = "DataType";

        private const string COLUMNSIZE = "ColumnSize";

        private const string NUMERICPRECISION = "NumericPrecision";

        private const string NUMERICSCALE = "NumericScale";

        private const int DTS_PIPELINE_CTR_ROWSWRITTEN = 103;

        private int TIMEOUT_SECONDS = 30;

        private DbProviderFactory m_DbFactory;

        private DbConnection m_DbConnection;

        private bool m_isConnected;

        private DbDataAdapter m_DbAdapter;

        private DataTable m_table;

        private SqlBulkCopy m_sqlBulkCopy;

        private DataColumn[] m_tableCols;

        private DbCommand m_insertCmd;

        private DTSRowDisposition m_errorRowDisposition;

        private bool m_isSkipMode;

        private int[] m_bufferIdxs;

        private int m_batchSize;

        private MyADONetDestination.ProviderDescriptorTable m_providerDesTbl;

        private MyADONetDestination.ProviderDescriptorsProviderDescriptor m_descriptor;

        private int m_commandTimeout;

        private bool m_useBulkInsert;

        private int m_cRowsInserted;

        private bool m_HaveUpstreamRows;

        private string m_fullTableName;

        private string m_tableNameLvl3;

        private string m_tableNameLvl2;

        private string m_tableNameLvl1;

        public MyADONetDestination()
        {
        }

        public override void AcquireConnections(object transaction)
        {
            bool flag;
            IDTSRuntimeConnection100 item;
            bool flag1;
            bool flag2;
            object obj;
            bool flag3;
            bool flag4;
            if (this.m_isConnected)
            {
                base.ErrorSupport.FireError(-1071636470, out flag);
                throw new PipelineComponentHResultException(-1071636470);
            }
            try
            {
                item = base.ComponentMetaData.RuntimeConnectionCollection["IDbConnection"];
            }
            catch (Exception exception)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] objArray = new object[] { "IDbConnection" };
                errorSupport.FireErrorWithArgs(-1071611878, out flag1, objArray);
                throw new PipelineComponentHResultException(-1071611878);
            }
            IDTSConnectionManager100 connectionManager = item.ConnectionManager;
            if (connectionManager == null)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] objArray1 = new object[] { "IDbConnection" };
                errorSupport1.FireErrorWithArgs(-1071611851, out flag2, objArray1);
                throw new PipelineComponentHResultException(-1071611851);
            }
            try
            {
                obj = connectionManager.AcquireConnection(transaction);
            }
            catch (Exception exception2)
            {
                Exception exception1 = exception2;
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
                object[] connectionManagerID = new object[] { item.ConnectionManagerID, exception1.Message };
                errorSupport2.FireErrorWithArgs(-1071610798, out flag3, connectionManagerID);
                throw new PipelineComponentHResultException(exception1.Message, -1071610798);
            }
            this.m_DbConnection = obj as DbConnection;
            if (this.m_DbConnection == null)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport3 = base.ErrorSupport;
                object[] connectionManagerID1 = new object[] { item.ConnectionManagerID };
                errorSupport3.FireErrorWithArgs(-1071610797, out flag4, connectionManagerID1);
                DtsConvert.GetWrapper(connectionManager).ReleaseConnection(obj);
                throw new PipelineComponentHResultException(-1071610797);
            }
            this.m_isConnected = true;
        }
        private void AdjustParameterType(DbParameter parameter, Type type)
        {
            if (type.Equals(typeof(byte[])))
            {
                parameter.DbType = DbType.Binary;
            }
        }

        private void AssureExternalColumnDataTypeConsistency()
        {
            if (base.ComponentMetaData.InputCollection.Count == 0)
            {
                return;
            }
            foreach (IDTSExternalMetadataColumn100 externalMetadataColumnCollection in base.ComponentMetaData.InputCollection[0].ExternalMetadataColumnCollection)
            {
                MyADONetDestination.FixInconsistentColumnMetadata(externalMetadataColumnCollection);
            }
        }

        private void checkTypes(IDTSInputColumn100 iDTSInpCol, IDTSExternalMetadataColumn100 iDTSExtCol)
        {
            bool flag = false;
            DataType fitManaged = PipelineComponent.ConvertBufferDataTypeToFitManaged(iDTSInpCol.DataType, ref flag);
            DataType dataType = iDTSExtCol.DataType;
            if (fitManaged == dataType && (fitManaged == DataType.DT_WSTR || fitManaged == DataType.DT_BYTES) && iDTSInpCol.Length > iDTSExtCol.Length)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] name = new object[] { iDTSInpCol.Name, iDTSInpCol.Length, iDTSExtCol.Name, iDTSExtCol.Length };
                errorSupport.FireWarningWithArgs(-2145348953, name);
            }
            if (!PipelineComponent.IsCompatibleNumericTypes(fitManaged, dataType))
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] objArray = new object[] { iDTSInpCol.Name, Enum.GetName(typeof(DataType), fitManaged), iDTSExtCol.Name, Enum.GetName(typeof(DataType), dataType) };
                errorSupport1.FireWarningWithArgs(-2145348948, objArray);
            }
        }

        private static bool CodePageIgnored(DataType dataType)
        {
            if (dataType != DataType.DT_STR && dataType != DataType.DT_TEXT)
            {
                return true;
            }
            return false;
        }

        private DataTable ConvertConnMetadataTbl(DataTable oldTbl, DataColumn nameCol, DataColumn typeCol, DataColumn sizeCol, DataColumn precisionCol, DataColumn scaleCol)
        {
            DataTable dataTable;
            DataTable schema = null;
            DataTable dataTable1 = null;
            Hashtable hashtables = null;
            try
            {
                try
                {
                    object item = null;
                    item = oldTbl.Rows[0][typeCol];
                    bool flag = (item is int ? true : false);
                    if (!flag && !(item is string))
                    {
                        throw new Exception();
                    }
                    schema = this.m_DbConnection.GetSchema("DataTypes");
                    hashtables = new Hashtable(schema.Rows.Count);
                    DataColumn dataColumn = (flag ? schema.Columns["ProviderDbType"] : schema.Columns["TypeName"]);
                    foreach (DataRow row in schema.Rows)
                    {
                        if (hashtables.Contains(row[dataColumn]))
                        {
                            continue;
                        }
                        hashtables.Add(row[dataColumn], Type.GetType((string)row["DataType"]));
                    }
                    dataTable1 = new DataTable()
                    {
                        Locale = CultureInfo.InvariantCulture
                    };
                    DataColumn[] dataColumnArray = new DataColumn[] { new DataColumn("ColumnName", typeof(string)), new DataColumn("DataType", typeof(Type)), new DataColumn("ColumnSize", typeof(int)), new DataColumn("NumericPrecision", typeof(short)), new DataColumn("NumericScale", typeof(short)) };
                    dataTable1.Columns.AddRange(dataColumnArray);
                    foreach (DataRow dataRow in oldTbl.Rows)
                    {
                        DataRow value = dataTable1.NewRow();
                        value[0] = (string)dataRow[nameCol];
                        item = dataRow[typeCol];
                        item = hashtables[item];
                        if (item == null)
                        {
                            throw new Exception();
                        }
                        value[1] = (Type)item;
                        value[2] = DBNull.Value;
                        value[3] = DBNull.Value;
                        value[4] = DBNull.Value;
                        item = dataRow[sizeCol];
                        if (!(item is DBNull))
                        {
                            value[2] = Convert.ToInt32(item, CultureInfo.InvariantCulture);
                        }
                        item = dataRow[precisionCol];
                        if (!(item is DBNull))
                        {
                            value[3] = Convert.ToInt16(item, CultureInfo.InvariantCulture);
                        }
                        item = dataRow[scaleCol];
                        if (!(item is DBNull))
                        {
                            value[4] = Convert.ToInt16(item, CultureInfo.InvariantCulture);
                        }
                        dataTable1.Rows.Add(value);
                    }
                    dataTable = dataTable1;
                }
                catch (Exception exception)
                {
                    if (dataTable1 != null)
                    {
                        dataTable1.Clear();
                    }
                    dataTable = null;
                }
            }
            finally
            {
                if (schema != null)
                {
                    schema.Clear();
                }
                oldTbl.Clear();
                hashtables.Clear();
            }
            return dataTable;
        }

        private void CreateSqlBulkCopyObject() 
        {
            this.m_sqlBulkCopy = new SqlBulkCopy(this.m_DbConnection as SqlConnection)
            {
                DestinationTableName = this.m_fullTableName,
                BulkCopyTimeout = this.m_commandTimeout
            };
        }

        private static bool DataTypeLengthIgnored(DataType dataType)
        {
            if (dataType != DataType.DT_STR && dataType != DataType.DT_WSTR && dataType != DataType.DT_BYTES)
            {
                return true;
            }
            return false;
        }

        private static void FixInconsistentColumnMetadata(IDTSExternalMetadataColumn100 column)
        {

            if (MyADONetDestination.DataTypeLengthIgnored(column.DataType))
            {
                column.Length = 0;
            }
            if (MyADONetDestination.PrecisionIgnored(column.DataType))
            {
                column.Precision = 0;
            }
            if (MyADONetDestination.ScaleIgnored(column.DataType))
            {
                column.Scale = 0;
            }
            if (MyADONetDestination.CodePageIgnored(column.DataType))
            {
                column.CodePage = 0;
            }
        }

        private object GetBufferDataAtCol(PipelineBuffer buffer, int iCol)
        {
            object item;
            int mBufferIdxs = this.m_bufferIdxs[iCol];
            if (!buffer.IsNull(mBufferIdxs))
            {
                item = buffer[mBufferIdxs];
                BlobColumn blobColumn = item as BlobColumn;
                if (blobColumn != null)
                {
                    DataType dataType = blobColumn.ColumnInfo.DataType;
                    if (dataType == DataType.DT_TEXT || dataType == DataType.DT_NTEXT)
                    {
                        item = buffer.GetString(mBufferIdxs);
                    }
                    else if (dataType == DataType.DT_IMAGE)
                    {
                        item = blobColumn.GetBlobData(0, (int)blobColumn.Length);
                    }
                }
            }
            else
            {
                item = DBNull.Value;
            }
            return item;
        }

        private int GetComponentVersion()
        {
            DtsPipelineComponentAttribute customAttribute = (DtsPipelineComponentAttribute)Attribute.GetCustomAttribute(base.GetType(), typeof(DtsPipelineComponentAttribute), false);
            return customAttribute.CurrentVersion;
        }

        private DbProviderFactory GetDbFactory()
        {
            bool flag;
            DbProviderFactory dbProviderFactory;
            Type type = this.m_DbConnection.GetType();
            DataTable factoryClasses = DbProviderFactories.GetFactoryClasses();
            DataColumn item = factoryClasses.Columns["InvariantName"];
            IEnumerator enumerator = factoryClasses.Rows.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    string str = (string)((DataRow)enumerator.Current)[item];
                    try
                    {
                        DbProviderFactory factory = DbProviderFactories.GetFactory(str);
                        DbConnection dbConnection = factory.CreateConnection();
                        if (!dbConnection.GetType().Equals(type))
                        {
                            dbConnection.Dispose();
                        }
                        else
                        {
                            dbConnection.Dispose();
                            factoryClasses.Clear();
                            dbProviderFactory = factory;
                            return dbProviderFactory;
                        }
                    }
                    catch (ConfigurationException configurationException1)
                    {
                        ConfigurationException configurationException = configurationException1;
                        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                        object[] message = new object[] { str, configurationException.Message };
                        errorSupport.FireWarningWithArgs(-2145348837, message);
                    }
                }
                factoryClasses.Clear();
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] objArray = new object[] { this.m_DbConnection.GetType().ToString() };
                errorSupport1.FireErrorWithArgs(-1071610804, out flag, objArray);
                throw new PipelineComponentHResultException(-1071610804);
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            return dbProviderFactory;
        }

        private DataTable GetMetadataTableByCommand(DbCommand command)
        {
            DbDataReader dbDataReaders;
            DataTable schemaTable = null;
            this.PostDiagnosticMessage("DbCommand.ExecuteReader");
            try
            {
                dbDataReaders = command.ExecuteReader(CommandBehavior.SchemaOnly);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.PostDiagnosticMessage("DbCommand.ExecuteReader failed.");
                throw exception;
            }
            this.PostDiagnosticMessage("DbCommand.ExecuteReader succeeded");
            this.PostDiagnosticMessage("DbDataReader.GetSchemaTable");
            schemaTable = dbDataReaders.GetSchemaTable();
            this.PostDiagnosticMessage("DbDataReader.GetSchemaTable finished");
            dbDataReaders.Close();
            dbDataReaders.Dispose();
            if (schemaTable == null || schemaTable.Rows.Count == 0)
            {
                this.PostDiagnosticMessage("DbDataReader.GetSchemaTable returned empty table. ");
                throw new Exception("The provider returned an empty schema table.");
            }
            return schemaTable;
        }

        private DataTable GetMetadataTableByConnection()
        {
            string str;
            DataTable schema = null;
            string[] strArrays = null;
            DataTable dataTable = this.m_DbConnection.GetSchema("Restrictions");
            int num = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                if ((string)row["CollectionName"] != "Columns")
                {
                    continue;
                }
                num++;
            }
            if (num == 4)
            {
                string[] mTableNameLvl3 = new string[] { this.m_tableNameLvl3, this.m_tableNameLvl2, this.m_tableNameLvl1, null };
                strArrays = mTableNameLvl3;
            }
            else if (num == 3)
            {
                string[] mTableNameLvl2 = new string[] { this.m_tableNameLvl2, this.m_tableNameLvl1, null };
                strArrays = mTableNameLvl2;
            }
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\110\\SSIS\\Setup\\DTSPath"))
            {
                str = string.Concat(registryKey.GetValue(string.Empty) as string, "ProviderDescriptors\\");
            }
            if (this.m_providerDesTbl == null)
            {
                this.m_providerDesTbl = new MyADONetDestination.ProviderDescriptorTable();
            }
            this.m_descriptor = this.m_providerDesTbl.GetDescriptor(this.m_DbConnection.GetType().ToString());
            if (this.m_descriptor == null)
            {
                throw new Exception("Failed to load schema.");
            }
            this.PostDiagnosticMessage("DbConnection.GetSchema");
            try
            {
                schema = this.m_DbConnection.GetSchema(this.m_descriptor.SchemaNames.ColumnsSchemaName, strArrays);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.PostDiagnosticMessage("DbConnection.GetSchema failed. ");
                throw exception;
            }
            this.PostDiagnosticMessage("DbConnection.GetSchema succeeded");
            if (schema == null || schema.Rows.Count == 0)
            {
                throw new Exception("Connection returned null schema");
            }
            DataColumn item = null;
            DataColumn dataColumn = null;
            DataColumn item1 = null;
            DataColumn dataColumn1 = null;
            DataColumn item2 = null;
            item = schema.Columns[this.m_descriptor.ColumnSchemaAttributes.NameColumnName];
            if (item == null)
            {
                throw new Exception("ColumnnameDescriptorInvalid");
            }
            dataColumn = schema.Columns[this.m_descriptor.ColumnSchemaAttributes.DataTypeColumnName];
            if (dataColumn == null)
            {
                throw new Exception("DatatypeDescriptorInvalid");
            }
            item1 = schema.Columns[this.m_descriptor.ColumnSchemaAttributes.MaximumLengthColumnName];
            if (item1 == null)
            {
                throw new Exception("ColunsizeDescriptorInvalid");
            }
            dataColumn1 = schema.Columns[this.m_descriptor.ColumnSchemaAttributes.NumericPrecisionColumnName];
            if (dataColumn1 == null)
            {
                throw new Exception("NumericPrecisionDescriptorInvalid");
            }
            item2 = schema.Columns[this.m_descriptor.ColumnSchemaAttributes.NumericScaleColumnName];
            if (item2 == null)
            {
                throw new Exception("NumericaScaleInvalid");
            }
            schema = this.ConvertConnMetadataTbl(schema, item, dataColumn, item1, dataColumn1, item2);
            if (schema == null || schema.Rows.Count == 0)
            {
                throw new Exception("FailureConvertTable");
            }
            return schema;
        }

        private string getParmameterMarkerFormat()
        {
            if (this.m_DbConnection.GetType().Equals(typeof(SqlConnection)))
            {
                return "@{0}";
            }
            DataTable schema = this.m_DbConnection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
            return (string)schema.Rows[0]["ParameterMarkerFormat"];
        }

        private string GetTypeAssemblyString(object obj)
        {
            return string.Concat(obj.GetType().ToString(), " from ", obj.GetType().Assembly.ToString());
        }

        public override void PerformUpgrade(int pipelineVersion)
        {
            int componentVersion = this.GetComponentVersion();
            if (base.ComponentMetaData.Version < componentVersion)
            {
                if (base.ComponentMetaData.Version < 1)
                {
                    bool flag = false;
                    int num = 0;
                    while (num < base.ComponentMetaData.CustomPropertyCollection.Count)
                    {
                        if (!base.ComponentMetaData.CustomPropertyCollection[num].Name.Equals("UseBulkInsertWhenPossible", StringComparison.Ordinal))
                        {
                            num++;
                        }
                        else
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        IDTSCustomProperty100 useBulkInsertDescription = base.ComponentMetaData.CustomPropertyCollection.New();
                        useBulkInsertDescription.Name = "UseBulkInsertWhenPossible";
                        useBulkInsertDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
                        useBulkInsertDescription.Description = OakGov.Etl.ZumoDestination.Localized.UseBulkInsertDescription;
                        useBulkInsertDescription.Value = false;
                    }
                }
                if (base.ComponentMetaData.Version < 2)
                {
                    this.AssureExternalColumnDataTypeConsistency();
                }
                base.ComponentMetaData.Version = componentVersion;
            }
        }

        private void PostDiagnosticMessage(string message)
        {
            byte[] numArray = null;
            base.ComponentMetaData.PostLogMessage("Diagnostic", null, message, DateTime.Now, DateTime.Now, 0, ref numArray);
        }

        public override void PostExecute()
        {
            if (this.m_DbAdapter.ContinueUpdateOnError && this.m_HaveUpstreamRows && this.m_cRowsInserted == 0)
            {
                base.ErrorSupport.FireWarning(-2145348949);
            }
            base.PostExecute();
            if (this.m_table != null)
            {
                this.m_table.Dispose();
                this.m_table = null;
            }
            this.m_bufferIdxs = null;
            this.m_tableCols = null;
            if (this.m_DbAdapter != null)
            {
                this.m_DbAdapter.Dispose();
                this.m_DbAdapter = null;
            }
        }

        private static bool PrecisionIgnored(DataType dataType)
        {
            if (dataType == DataType.DT_NUMERIC)
            {
                return false;
            }
            return true;
        }

        public override void PreExecute()
        {
            bool flag;
            bool flag1;
            bool flag2;
            bool flag3;
            bool flag4;
            string str;
            base.PreExecute();
            if (!this.m_isConnected)
            {
                base.ErrorSupport.FireError(-1071636446, out flag);
                throw new PipelineComponentHResultException(-1071636446);
            }
            object value = base.ComponentMetaData.CustomPropertyCollection["CommandTimeout"].Value;
            if (!(value is int) || (int)value < 0)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] identificationString = new object[] { "CommandTimeout", base.ComponentMetaData.IdentificationString };
                errorSupport.FireErrorWithArgs(-1071607640, out flag1, identificationString);
                throw new PipelineComponentHResultException(-1071607640);
            }
            this.m_commandTimeout = (int)value;
            this.m_DbFactory = this.GetDbFactory();
            IDTSCustomProperty100 item = base.ComponentMetaData.CustomPropertyCollection["TableOrViewName"];
            if (item.Value == null)
            {
                str = null;
            }
            else
            {
                str = item.Value.ToString().Trim();
            }
            this.m_fullTableName = str;
            if (string.IsNullOrEmpty(this.m_fullTableName))
            {
                base.ErrorSupport.FireError(-1071636414, out flag2);
                throw new PipelineComponentHResultException(-1071636414);
            }
            MyADONetDestination.QuoteUtil quoteUtil = new MyADONetDestination.QuoteUtil(this.m_DbConnection);
            if (!quoteUtil.GetValidTableName(this.m_fullTableName, out this.m_tableNameLvl3, out this.m_tableNameLvl2, out this.m_tableNameLvl1))
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] prefix = new object[] { quoteUtil.Prefix, quoteUtil.Sufix };
                errorSupport1.FireErrorWithArgs(-1071610789, out flag3, prefix);
                throw new PipelineComponentHResultException(-1071610789);
            }
            this.m_table = new DataTable()
            {
                Locale = CultureInfo.InvariantCulture
            };
            IDTSInput100 dTSInput100 = base.ComponentMetaData.InputCollection[0];
            IDTSExternalMetadataColumnCollection100 externalMetadataColumnCollection = dTSInput100.ExternalMetadataColumnCollection;
            IDTSInputColumnCollection100 inputColumnCollection = dTSInput100.InputColumnCollection;
            int count = inputColumnCollection.Count;
            this.m_tableCols = new DataColumn[count];
            IDTSExternalMetadataColumn100[] dTSExternalMetadataColumn100Array = new IDTSExternalMetadataColumn100[count];
            this.m_bufferIdxs = new int[count];
            for (int i = 0; i < count; i++)
            {
                IDTSInputColumn100 dTSInputColumn100 = inputColumnCollection[i];
                IDTSExternalMetadataColumn100 dTSExternalMetadataColumn100 = externalMetadataColumnCollection.FindObjectByID(dTSInputColumn100.ExternalMetadataColumnID);
                dTSExternalMetadataColumn100Array[i] = dTSExternalMetadataColumn100;
                DataType dataType = dTSInputColumn100.DataType;
                bool flag5 = false;
                dataType = PipelineComponent.ConvertBufferDataTypeToFitManaged(dataType, ref flag5);
                Type dataRecordType = PipelineComponent.BufferTypeToDataRecordType(dataType);
                this.m_tableCols[i] = new DataColumn(dTSExternalMetadataColumn100.Name, dataRecordType);
                int lineageID = dTSInputColumn100.LineageID;
                try
                {
                    this.m_bufferIdxs[i] = base.BufferManager.FindColumnByLineageID(dTSInput100.Buffer, lineageID);
                }
                catch (Exception exception)
                {
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
                    object[] name = new object[] { lineageID, dTSExternalMetadataColumn100.Name };
                    errorSupport2.FireErrorWithArgs(-1071610795, out flag4, name);
                    throw new PipelineComponentHResultException(-1071610795);
                }
            }
            this.m_table.Columns.AddRange(this.m_tableCols);
            this.PostDiagnosticMessage(OakGov.Etl.ZumoDestination.Localized.DiagnosticPre);
            this.m_insertCmd = this.m_DbFactory.CreateCommand();
            this.PostDiagnosticMessage(OakGov.Etl.ZumoDestination.Localized.DiagnosticPost);
            this.m_insertCmd.Connection = this.m_DbConnection;
            this.m_insertCmd.CommandTimeout = this.m_commandTimeout;
            this.m_insertCmd.UpdatedRowSource = UpdateRowSource.None;
            int num = count - 1;
            StringBuilder stringBuilder = new StringBuilder("INSERT INTO ");
            stringBuilder.Append(this.m_fullTableName);
            stringBuilder.Append(" (");
            for (int j = 0; j < num; j++)
            {
                stringBuilder.Append(quoteUtil.QuoteName(dTSExternalMetadataColumn100Array[j].Name));
                stringBuilder.Append(", ");
            }
            stringBuilder.Append(quoteUtil.QuoteName(dTSExternalMetadataColumn100Array[num].Name));
            stringBuilder.Append(") VALUES (");
            string parmameterMarkerFormat = this.getParmameterMarkerFormat();
            this.PostDiagnosticMessage(OakGov.Etl.ZumoDestination.Localized.DiagnosticPre);
            string empty = string.Empty;
            for (int k = 0; k <= num; k++)
            {
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;
                object[] objArray = new object[] { "p", k + 1 };
                empty = string.Format(invariantCulture, "{0}{1:d}", objArray);
                CultureInfo cultureInfo = CultureInfo.InvariantCulture;
                object[] objArray1 = new object[] { empty };
                empty = string.Format(cultureInfo, parmameterMarkerFormat, objArray1);
                DbParameter columnName = this.m_DbFactory.CreateParameter();
                columnName.ParameterName = empty;
                columnName.SourceColumn = this.m_table.Columns[k].ColumnName;
                this.AdjustParameterType(columnName, this.m_table.Columns[k].DataType);
                this.m_insertCmd.Parameters.Add(columnName);
                stringBuilder.Append(empty);
                if (k != num)
                {
                    stringBuilder.Append(", ");
                }
                else
                {
                    stringBuilder.Append(")");
                }
            }
            this.PostDiagnosticMessage(Localized.DiagnosticPost);
            this.m_insertCmd.CommandText = stringBuilder.ToString();
            this.PostDiagnosticMessage(Localized.DiagnosticPre);
            this.m_DbAdapter = this.m_DbFactory.CreateDataAdapter();
            this.PostDiagnosticMessage(Localized.DiagnosticPost);
            this.m_DbAdapter.InsertCommand = this.m_insertCmd;
            IDTSCustomProperty100 dTSCustomProperty100 = base.ComponentMetaData.CustomPropertyCollection["BatchSize"];
            this.m_batchSize = (int)dTSCustomProperty100.Value;
            if (this.m_batchSize >= 0 && this.m_batchSize != 1)
            {
                try
                {
                    this.PostDiagnosticMessage(Localized.DiagnosticPre);
                    this.m_DbAdapter.UpdateBatchSize = this.m_batchSize;
                    this.PostDiagnosticMessage(Localized.DiagnosticPost);
                }
                catch (Exception exception2)
                {
                    Exception exception1 = exception2;
                    this.PostDiagnosticMessage(Localized.DiagnosticPost);
                }
            }
            this.m_errorRowDisposition = dTSInput100.ErrorRowDisposition;
            this.PostDiagnosticMessage(Localized.DiagnosticPre);
            if (this.m_errorRowDisposition == DTSRowDisposition.RD_IgnoreFailure || this.m_errorRowDisposition == DTSRowDisposition.RD_RedirectRow)
            {
                this.m_DbAdapter.ContinueUpdateOnError = true;
            }
            else
            {
                this.m_DbAdapter.ContinueUpdateOnError = false;
            }
            this.PostDiagnosticMessage(Localized.DiagnosticPost);
            this.m_cRowsInserted = 0;
            this.m_HaveUpstreamRows = false;
            if (this.m_useBulkInsert)
            {
                this.CreateSqlBulkCopyObject();
                for (int l = 0; l < count; l++)
                {
                    SqlBulkCopyColumnMapping sqlBulkCopyColumnMapping = new SqlBulkCopyColumnMapping(l, dTSExternalMetadataColumn100Array[l].Name);
                    this.m_sqlBulkCopy.ColumnMappings.Add(sqlBulkCopyColumnMapping);
                }
            }
            this.m_isSkipMode = false;
            try
            {
                IDTSCustomProperty100 item1 = base.ComponentMetaData.CustomPropertyCollection["SkipInsertMode"];
                this.m_isSkipMode = (bool)item1.Value;
            }
            catch (Exception exception3)
            {
            }
        }

        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            int count = this.m_table.Columns.Count;
            if (this.m_batchSize == 0)
            {
                this.m_batchSize = buffer.RowCount;
            }
            this.m_HaveUpstreamRows = (buffer.RowCount > 0 ? true : this.m_HaveUpstreamRows);
            while (buffer.NextRow())
            {
                DataRow bufferDataAtCol = this.m_table.NewRow();
                for (int i = 0; i < count; i++)
                {
                    bufferDataAtCol[this.m_tableCols[i]] = this.GetBufferDataAtCol(buffer, i);
                }
                this.m_table.Rows.Add(bufferDataAtCol);
                if (this.m_table.Rows.Count < this.m_batchSize)
                {
                    continue;
                }
                int currentRow = buffer.CurrentRow - this.m_table.Rows.Count + 1;
                this.SendDataToDestination(buffer, currentRow);
                this.m_table.Rows.Clear();
            }
            if (this.m_table.Rows.Count > 0)
            {
                int rowCount = buffer.RowCount - this.m_table.Rows.Count;
                this.SendDataToDestination(buffer, rowCount);
                this.m_table.Rows.Clear();
            }
        }

        public override void ProvideComponentProperties()
        {
            IDTSRuntimeConnection100 item;
            base.RemoveAllInputsOutputsAndCustomProperties();
            base.ComponentMetaData.Name = Localized.ComponentName;
            base.ComponentMetaData.Description = Localized.ComponentDescription;
            IDTSComponentMetaData100 componentMetaData = base.ComponentMetaData;
            string[] description = new string[] { base.ComponentMetaData.Description, Localized.ContactInfo1, " © ", Localized.ContactInfo2, null };
            int componentVersion = this.GetComponentVersion();
            description[4] = componentVersion.ToString(CultureInfo.InvariantCulture);
            componentMetaData.ContactInfo = string.Concat(description);
            try
            {
                item = base.ComponentMetaData.RuntimeConnectionCollection["IDbConnection"];
            }
            catch (Exception exception)
            {
                item = base.ComponentMetaData.RuntimeConnectionCollection.New();
                item.Name = "IDbConnection";
                item.Description = Localized.ConnectionDescription;
            }
            IDTSInput100 inputName = base.ComponentMetaData.InputCollection.New();
            inputName.Name = Localized.InputName;
            inputName.HasSideEffects = true;
            inputName.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;
            IDTSOutput100 errorOutputName = base.ComponentMetaData.OutputCollection.New();
            errorOutputName.Name = Localized.ErrorOutputName;
            errorOutputName.IsErrorOut = true;
            errorOutputName.SynchronousInputID = inputName.ID;
            errorOutputName.ExclusionGroup = 1;
            base.ComponentMetaData.UsesDispositions = true;
            inputName.ExternalMetadataColumnCollection.IsUsed = true;
            base.ComponentMetaData.ValidateExternalMetadata = true;
            base.ComponentMetaData.Version = this.GetComponentVersion();
            IDTSCustomProperty100 tableNameDescription = base.ComponentMetaData.CustomPropertyCollection.New();
            tableNameDescription.Name = "TableOrViewName";
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            object[] objArray = new object[] { "11.0.0.0" };
            tableNameDescription.UITypeEditor = string.Format(invariantCulture, "Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor, Microsoft.DataTransformationServices.Controls, Version= {0}, Culture=neutral, PublicKeyToken=89845dcd8080cc91", objArray);
            tableNameDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            tableNameDescription.Description = Localized.TableNameDescription;
            tableNameDescription.Value = string.Empty;
            IDTSCustomProperty100 batchSizeDescription = base.ComponentMetaData.CustomPropertyCollection.New();
            batchSizeDescription.Name = "BatchSize";
            batchSizeDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            batchSizeDescription.Description = Localized.BatchSizeDescription;
            batchSizeDescription.Value = 0;
            IDTSCustomProperty100 commandTimeoutDescription = base.ComponentMetaData.CustomPropertyCollection.New();
            commandTimeoutDescription.Name = "CommandTimeout";
            commandTimeoutDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            commandTimeoutDescription.Description = Localized.CommandTimeoutDescription;
            commandTimeoutDescription.Value = this.TIMEOUT_SECONDS;
            IDTSCustomProperty100 useBulkInsertDescription = base.ComponentMetaData.CustomPropertyCollection.New();
            useBulkInsertDescription.Name = "UseBulkInsertWhenPossible";
            useBulkInsertDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            useBulkInsertDescription.Description = Localized.UseBulkInsertDescription;
            useBulkInsertDescription.Value = true;
        }

        private void RedirectErrors(PipelineBuffer buffer, int startBuffIndex)
        {
            int d = base.ComponentMetaData.OutputCollection[0].ID;
            int count = this.m_table.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                if (this.m_useBulkInsert || this.m_table.Rows[i].HasErrors)
                {
                    buffer.DirectErrorRow(startBuffIndex + i, d, -1071610801, 0);
                }
            }
        }

        public override void ReinitializeMetaData()
        {
            bool flag;
            bool flag1;
            bool flag2;
            bool flag3;
            bool flag4;
            bool flag5;
            string str;
            base.ReinitializeMetaData();
            IDTSInput100 item = base.ComponentMetaData.InputCollection[0];
            if (!this.m_isConnected)
            {
                base.ErrorSupport.FireError(-1071636446, out flag);
                throw new PipelineComponentHResultException(-1071636446);
            }
            item.ExternalMetadataColumnCollection.RemoveAll();
            item.InputColumnCollection.RemoveAll();
            object value = base.ComponentMetaData.CustomPropertyCollection["CommandTimeout"].Value;
            if (!(value is int) || (int)value < 0)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] identificationString = new object[] { "CommandTimeout", base.ComponentMetaData.IdentificationString };
                errorSupport.FireErrorWithArgs(-1071607640, out flag1, identificationString);
                throw new PipelineComponentHResultException(-1071607640);
            }
            this.m_commandTimeout = (int)value;
            if (!(base.ComponentMetaData.CustomPropertyCollection["UseBulkInsertWhenPossible"].Value is bool))
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] objArray = new object[] { "UseBulkInsertWhenPossible", base.ComponentMetaData.IdentificationString };
                errorSupport1.FireErrorWithArgs(-1071607640, out flag2, objArray);
                throw new PipelineComponentHResultException(-1071607640);
            }
            this.m_DbFactory = this.GetDbFactory();
            this.PostDiagnosticMessage(Localized.DiagnosticPre);
            DbCommand mDbConnection = this.m_DbFactory.CreateCommand();
            this.PostDiagnosticMessage(Localized.DiagnosticPost);
            IDTSCustomProperty100 dTSCustomProperty100 = base.ComponentMetaData.CustomPropertyCollection["TableOrViewName"];
            if (dTSCustomProperty100.Value == null)
            {
                str = null;
            }
            else
            {
                str = dTSCustomProperty100.Value.ToString().Trim();
            }
            this.m_fullTableName = str;
            if (string.IsNullOrEmpty(this.m_fullTableName))
            {
                base.ErrorSupport.FireError(-1071636414, out flag3);
                throw new PipelineComponentHResultException(-1071636414);
            }
            MyADONetDestination.QuoteUtil quoteUtil = new MyADONetDestination.QuoteUtil(this.m_DbConnection);
            if (!quoteUtil.GetValidTableName(this.m_fullTableName, out this.m_tableNameLvl3, out this.m_tableNameLvl2, out this.m_tableNameLvl1))
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
                object[] prefix = new object[] { quoteUtil.Prefix, quoteUtil.Sufix };
                errorSupport2.FireErrorWithArgs(-1071610789, out flag4, prefix);
                throw new PipelineComponentHResultException(-1071610789);
            }
            mDbConnection.CommandText = string.Concat("select * from ", this.m_fullTableName);
            mDbConnection.Connection = this.m_DbConnection;
            mDbConnection.CommandTimeout = this.m_commandTimeout;
            DataTable metadataTableByCommand = null;
            bool flag6 = false;
            string empty = string.Empty;
            try
            {
                metadataTableByCommand = this.GetMetadataTableByCommand(mDbConnection);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.PostDiagnosticMessage(Localized.FailureGetMetadataTableByCommand);
                flag6 = true;
                empty = exception.Message;
            }
            if (flag6)
            {
                try
                {
                    metadataTableByCommand = this.GetMetadataTableByConnection();
                }
                catch (Exception exception3)
                {
                    Exception exception2 = exception3;
                    this.PostDiagnosticMessage(Localized.FailureGetMetadataTableByConnection);
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport3 = base.ErrorSupport;
                    object[] objArray1 = new object[1];
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    object[] message = new object[] { empty, exception2.Message };
                    objArray1[0] = string.Format(currentCulture, "\n{0}\n{1}", message);
                    errorSupport3.FireErrorWithArgs(-1071610793, out flag5, objArray1);
                    throw new PipelineComponentHResultException(-1071610793);
                }
            }
            this.SetExternalMetadataInfos(item.ExternalMetadataColumnCollection, metadataTableByCommand);
        }

        public override void ReleaseConnections()
        {
            if (this.m_DbConnection != null)
            {
                IDTSRuntimeConnection100 item = base.ComponentMetaData.RuntimeConnectionCollection["IDbConnection"];
                DtsConvert.GetWrapper(item.ConnectionManager).ReleaseConnection(this.m_DbConnection);
            }
            this.m_DbConnection = null;
            this.m_isConnected = false;
        }

        private static bool ScaleIgnored(DataType dataType)
        {
            if (dataType != DataType.DT_NUMERIC && dataType != DataType.DT_DECIMAL && dataType != DataType.DT_DBTIME2 && dataType != DataType.DT_DBTIMESTAMP2 && dataType != DataType.DT_DBTIMESTAMPOFFSET)
            {
                return true;
            }
            return false;
        }

        private void SendDataToDestination(PipelineBuffer buffer, int bufferIndex)
        {
            bool flag;
            bool flag1;
            if (this.m_isSkipMode)
            {
                return;
            }
            try
            {
                int count = 0;
                if (!this.m_useBulkInsert)
                {
                    this.PostDiagnosticMessage(Localized.DiagnosticPre);
                    count = this.m_DbAdapter.Update(this.m_table);
                    this.PostDiagnosticMessage(Localized.DiagnosticPost);
                }
                else
                {
                    try
                    {
                        this.PostDiagnosticMessage(Localized.DiagnosticPre);
                        this.m_sqlBulkCopy.WriteToServer(this.m_table);
                        this.PostDiagnosticMessage(Localized.DiagnosticPost);
                        count = this.m_table.Rows.Count;
                    }
                    catch (SqlException sqlException)
                    {
                        if (this.m_errorRowDisposition == DTSRowDisposition.RD_FailComponent)
                        {
                            throw;
                        }
                    }
                }
                if (count > 0)
                {
                    base.ComponentMetaData.IncrementPipelinePerfCounter(103, Convert.ToUInt32(count));
                    this.m_cRowsInserted += count;
                }
                if (count < this.m_table.Rows.Count && this.m_errorRowDisposition == DTSRowDisposition.RD_RedirectRow)
                {
                    this.RedirectErrors(buffer, bufferIndex);
                }
            }
            catch (ArgumentException argumentException1)
            {
                ArgumentException argumentException = argumentException1;
                this.PostDiagnosticMessage(string.Concat(Localized.DiagnosticPost, this.GetTypeAssemblyString(this.m_DbAdapter)));
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] message = new object[] { argumentException.Message };
                errorSupport.FireErrorWithArgs(-1071610803, out flag, message);
                throw new PipelineComponentHResultException(-1071610803);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.PostDiagnosticMessage(string.Concat(Localized.DiagnosticPost, this.GetTypeAssemblyString(this.m_DbAdapter)));
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] objArray = new object[] { exception.Message };
                errorSupport1.FireErrorWithArgs(-1071610805, out flag1, objArray);
                throw new PipelineComponentHResultException(-1071610805);
            }
        }

        private void SetExternalMetadataInfos(IDTSExternalMetadataColumnCollection100 iDTSExtCols, DataTable metadataTbl)
        {
            int count = metadataTbl.Rows.Count;
            string empty = string.Empty;
            int num = 0;
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            DataType dataType = DataType.DT_EMPTY;
            for (int i = 0; i < count; i++)
            {
                DataRow item = metadataTbl.Rows[i];
                IDTSExternalMetadataColumn100 dTSExternalMetadataColumn100 = iDTSExtCols.NewAt(i);
                this.SetMetadataValsFromRow(item, out empty, out num, out num1, out num2, out num3, out dataType);
                dTSExternalMetadataColumn100.Name = empty;
                dTSExternalMetadataColumn100.CodePage = num;
                dTSExternalMetadataColumn100.Length = num1;
                dTSExternalMetadataColumn100.Precision = num2;
                dTSExternalMetadataColumn100.Scale = num3;
                dTSExternalMetadataColumn100.DataType = dataType;
            }
            metadataTbl.Dispose();
        }

        private void SetMetadataValsFromRow(DataRow currRow, out string name, out int codePage, out int length, out int precision, out int scale, out DataType dtsType)
        {
            bool flag;
            name = (string)currRow["ColumnName"];
            Type item = (Type)currRow["DataType"];
            codePage = 0;
            length = 0;
            precision = 0;
            scale = 0;
            dtsType = DataType.DT_EMPTY;
            try
            {
                if (!item.Equals(typeof(object)))
                {
                    dtsType = PipelineComponent.DataRecordTypeToBufferType(item);
                }
                else
                {
                    dtsType = DataType.DT_NTEXT;
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                    object[] objArray = new object[] { name, base.ComponentMetaData.IdentificationString };
                    errorSupport.FireWarningWithArgs(-2145348831, objArray);
                }
            }
            catch (Exception exception)
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                object[] str = new object[] { item.ToString(), name, base.ComponentMetaData.IdentificationString };
                errorSupport1.FireErrorWithArgs(-1071610799, out flag, str);
                throw new PipelineComponentHResultException(-1071610799);
            }
            if ((int)dtsType == 130 || (int)dtsType == 128)
            {
                length = (int)currRow["ColumnSize"];
            }
            if ((int)dtsType == 131)
            {
                if (!currRow.IsNull("NumericPrecision"))
                {
                    precision = Convert.ToInt16(currRow["NumericPrecision"], CultureInfo.CurrentCulture);
                }
                else
                {
                    precision = 0;
                }
                if (currRow.IsNull("NumericScale"))
                {
                    scale = 0;
                    return;
                }
                scale = Convert.ToInt16(currRow["NumericScale"], CultureInfo.CurrentCulture);
            }
        }

        public override DTSValidationStatus Validate()
        {
            bool flag;
            bool flag1;
            bool flag2;
            bool flag3;
            bool flag4;
            bool flag5;
            bool flag6;
            bool flag7;
            bool flag8;
            bool flag9;
            IDTSExternalMetadataColumn100 dTSExternalMetadataColumn100;
            bool flag10;
            bool flag11;
            bool flag12;
            DTSValidationStatus dTSValidationStatu;
            string str;
            try
            {
                DTSValidationStatus dTSValidationStatu1 = base.Validate();
                if (dTSValidationStatu1 != DTSValidationStatus.VS_ISVALID)
                {
                    dTSValidationStatu = dTSValidationStatu1;
                }
                else if (base.ComponentMetaData.InputCollection.Count != 1)
                {
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                    object[] objArray = new object[] { 1 };
                    errorSupport.FireErrorWithArgs(-1071636454, out flag, objArray);
                    dTSValidationStatu = DTSValidationStatus.VS_ISCORRUPT;
                }
                else if (base.ComponentMetaData.OutputCollection.Count != 1)
                {
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                    object[] objArray1 = new object[] { 1 };
                    errorSupport1.FireErrorWithArgs(-1071636456, out flag1, objArray1);
                    dTSValidationStatu = DTSValidationStatus.VS_ISCORRUPT;
                }
                else if (base.ComponentMetaData.InputCollection[0].ErrorRowDisposition != DTSRowDisposition.RD_RedirectRow || base.ComponentMetaData.OutputCollection[0].IsErrorOut)
                {
                    IDTSInput100 item = base.ComponentMetaData.InputCollection[0];
                    if (item.TruncationRowDisposition == DTSRowDisposition.RD_NotUsed)
                    {
                        IDTSInputColumnCollection100 inputColumnCollection = item.InputColumnCollection;
                        int count = inputColumnCollection.Count;
                        if (count != 0)
                        {
                            int num = 0;
                            while (num < count)
                            {
                                if (inputColumnCollection[num].ErrorRowDisposition == DTSRowDisposition.RD_NotUsed)
                                {
                                    num++;
                                }
                                else
                                {
                                    base.ErrorSupport.FireError(-1071610792, out flag5);
                                    dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                    return dTSValidationStatu;
                                }
                            }
                            int num1 = 0;
                            while (num1 < count)
                            {
                                if (inputColumnCollection[num1].TruncationRowDisposition == DTSRowDisposition.RD_NotUsed)
                                {
                                    num1++;
                                }
                                else
                                {
                                    base.ErrorSupport.FireError(-1071610791, out flag6);
                                    dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                    return dTSValidationStatu;
                                }
                            }
                            object value = base.ComponentMetaData.CustomPropertyCollection["BatchSize"].Value;
                            if (value == null || !(value is int) || (int)value < 0)
                            {
                                base.ErrorSupport.FireError(-1071610802, out flag7);
                                dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                            }
                            else
                            {
                                object obj = base.ComponentMetaData.CustomPropertyCollection["CommandTimeout"].Value;
                                if (!(obj is int) || (int)obj < 0)
                                {
                                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
                                    object[] identificationString = new object[] { "CommandTimeout", base.ComponentMetaData.IdentificationString };
                                    errorSupport2.FireErrorWithArgs(-1071607640, out flag8, identificationString);
                                    dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                }
                                else
                                {
                                    this.m_commandTimeout = (int)obj;
                                    IDTSCustomProperty100 dTSCustomProperty100 = base.ComponentMetaData.CustomPropertyCollection["TableOrViewName"];
                                    if (dTSCustomProperty100.Value == null)
                                    {
                                        str = null;
                                    }
                                    else
                                    {
                                        str = dTSCustomProperty100.Value.ToString().Trim();
                                    }
                                    this.m_fullTableName = str;
                                    if (!string.IsNullOrEmpty(this.m_fullTableName))
                                    {
                                        IDTSExternalMetadataColumnCollection100 externalMetadataColumnCollection = item.ExternalMetadataColumnCollection;
                                        if (this.m_isConnected && base.ComponentMetaData.ValidateExternalMetadata)
                                        {
                                            dTSValidationStatu1 = this.ValidateWithExternalMetadata();
                                            if (dTSValidationStatu1 != DTSValidationStatus.VS_ISVALID)
                                            {
                                                dTSValidationStatu = dTSValidationStatu1;
                                                return dTSValidationStatu;
                                            }
                                        }
                                        for (int i = 0; i < count; i++)
                                        {
                                            IDTSInputColumn100 dTSInputColumn100 = inputColumnCollection[i];
                                            int externalMetadataColumnID = dTSInputColumn100.ExternalMetadataColumnID;
                                            try
                                            {
                                                dTSExternalMetadataColumn100 = externalMetadataColumnCollection.FindObjectByID(externalMetadataColumnID);
                                            }
                                            catch (COMException cOMException)
                                            {
                                                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport3 = base.ErrorSupport;
                                                object[] identificationString1 = new object[] { dTSInputColumn100.IdentificationString };
                                                errorSupport3.FireErrorWithArgs(-1071636259, out flag10, identificationString1);
                                                dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                                return dTSValidationStatu;
                                            }
                                            this.checkTypes(dTSInputColumn100, dTSExternalMetadataColumn100);
                                        }
                                        this.m_useBulkInsert = false;
                                        object value1 = base.ComponentMetaData.CustomPropertyCollection["UseBulkInsertWhenPossible"].Value;
                                        if (value1 is bool)
                                        {
                                            if ((bool)value1 && this.m_DbConnection is SqlConnection)
                                            {
                                                this.m_useBulkInsert = true;
                                                try
                                                {
                                                    this.CreateSqlBulkCopyObject();
                                                }
                                                catch (Exception exception1)
                                                {
                                                    Exception exception = exception1;
                                                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport4 = base.ErrorSupport;
                                                    object[] message = new object[] { exception.Message, base.ComponentMetaData.IdentificationString };
                                                    errorSupport4.FireErrorWithArgs(-1071610787, out flag12, message);
                                                    dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                                    return dTSValidationStatu;
                                                }
                                            }
                                            dTSValidationStatu = dTSValidationStatu1;
                                        }
                                        else
                                        {
                                            Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport5 = base.ErrorSupport;
                                            object[] identificationString2 = new object[] { "UseBulkInsertWhenPossible", base.ComponentMetaData.IdentificationString };
                                            errorSupport5.FireErrorWithArgs(-1071607640, out flag11, identificationString2);
                                            dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                        }
                                    }
                                    else
                                    {
                                        base.ErrorSupport.FireError(-1071636414, out flag9);
                                        dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport6 = base.ErrorSupport;
                            object[] objArray2 = new object[] { item.IdentificationString };
                            errorSupport6.FireErrorWithArgs(-1071636453, out flag4, objArray2);
                            dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                        }
                    }
                    else
                    {
                        base.ErrorSupport.FireError(-1071610790, out flag3);
                        dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                    }
                }
                else
                {
                    base.ErrorSupport.FireError(-1071610796, out flag2);
                    dTSValidationStatu = DTSValidationStatus.VS_ISCORRUPT;
                }
            }
            catch (Exception exception2)
            {
                dTSValidationStatu = DTSValidationStatus.VS_ISCORRUPT;
            }
            return dTSValidationStatu;
        }

        private DTSValidationStatus ValidateWithExternalMetadata()
        {
            bool flag;
            bool flag1;
            bool flag2;
            bool flag3;
            DTSValidationStatus dTSValidationStatu;
            MyADONetDestination.QuoteUtil quoteUtil = new MyADONetDestination.QuoteUtil(this.m_DbConnection);
            if (!quoteUtil.GetValidTableName(this.m_fullTableName, out this.m_tableNameLvl3, out this.m_tableNameLvl2, out this.m_tableNameLvl1))
            {
                Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
                object[] prefix = new object[] { quoteUtil.Prefix, quoteUtil.Sufix };
                errorSupport.FireErrorWithArgs(-1071610789, out flag, prefix);
                return DTSValidationStatus.VS_ISBROKEN;
            }
            DataTable metadataTableByCommand = null;
            bool flag4 = false;
            string empty = string.Empty;
            try
            {
                DbProviderFactory dbFactory = this.GetDbFactory();
                this.PostDiagnosticMessage(Localized.DiagnosticPre);
                DbCommand mDbConnection = dbFactory.CreateCommand();
                this.PostDiagnosticMessage(Localized.DiagnosticPost);
                mDbConnection.CommandText = string.Concat("select * from ", this.m_fullTableName);
                mDbConnection.Connection = this.m_DbConnection;
                mDbConnection.CommandTimeout = this.m_commandTimeout;
                metadataTableByCommand = this.GetMetadataTableByCommand(mDbConnection);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.PostDiagnosticMessage(Localized.FailureGetMetadataTableByCommand);
                flag4 = true;
                empty = exception.Message;
            }
            if (flag4)
            {
                try
                {
                    metadataTableByCommand = this.GetMetadataTableByConnection();
                    goto Label0;
                }
                catch (Exception exception3)
                {
                    Exception exception2 = exception3;
                    this.PostDiagnosticMessage(Localized.FailureGetMetadataTableByConnection);
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
                    object[] objArray = new object[1];
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    object[] message = new object[] { empty, exception2.Message };
                    objArray[0] = string.Format(currentCulture, "\n{0}\n{1}", message);
                    errorSupport1.FireErrorWithArgs(-1071610793, out flag1, objArray);
                    dTSValidationStatu = DTSValidationStatus.VS_ISBROKEN;
                }
                return dTSValidationStatu;
            }
        Label0:
            IDTSInputColumnCollection100 inputColumnCollection = base.ComponentMetaData.InputCollection[0].InputColumnCollection;
            IDTSExternalMetadataColumnCollection100 externalMetadataColumnCollection = base.ComponentMetaData.InputCollection[0].ExternalMetadataColumnCollection;
            int count = metadataTableByCommand.Rows.Count;
            Hashtable hashtables = new Hashtable(count);
            for (int i = 0; i < count; i++)
            {
                DataRow item = metadataTableByCommand.Rows[i];
                hashtables.Add((string)item["ColumnName"], i);
            }
            bool[] flagArray = new bool[count];
            for (int j = 0; j < count; j++)
            {
                flagArray[j] = false;
            }
            int num = inputColumnCollection.Count;
            Hashtable hashtables1 = new Hashtable(num);
            for (int k = 0; k < num; k++)
            {
                hashtables1.Add(inputColumnCollection[k].ExternalMetadataColumnID, null);
            }
            string str = string.Empty;
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            DataType dataType = DataType.DT_EMPTY;
            int count1 = externalMetadataColumnCollection.Count;
            if (count1 == 0)
            {
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }
            for (int l = 0; l < count1; l++)
            {
                IDTSExternalMetadataColumn100 dTSExternalMetadataColumn100 = externalMetadataColumnCollection[l];
                object obj = hashtables[dTSExternalMetadataColumn100.Name];
                if (obj != null)
                {
                    int num5 = (int)obj;
                    flagArray[num5] = true;
                    DataRow dataRow = metadataTableByCommand.Rows[num5];
                    this.SetMetadataValsFromRow(dataRow, out str, out num1, out num2, out num3, out num4, out dataType);
                    string empty1 = string.Empty;
                    if (dTSExternalMetadataColumn100.CodePage != num1)
                    {
                        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                        object[] objArray1 = new object[] { num1 };
                        empty1 = string.Concat(empty1, string.Format(cultureInfo, "new code page: {0} ", objArray1));
                    }
                    if (dTSExternalMetadataColumn100.Length != num2)
                    {
                        CultureInfo currentCulture1 = CultureInfo.CurrentCulture;
                        object[] objArray2 = new object[] { num2 };
                        empty1 = string.Concat(empty1, string.Format(currentCulture1, "new length: {0} ", objArray2));
                    }
                    if (dTSExternalMetadataColumn100.Precision != num3)
                    {
                        CultureInfo cultureInfo1 = CultureInfo.CurrentCulture;
                        object[] objArray3 = new object[] { num3 };
                        empty1 = string.Concat(empty1, string.Format(cultureInfo1, "new precision: {0} ", objArray3));
                    }
                    if (dTSExternalMetadataColumn100.Scale != num4)
                    {
                        CultureInfo currentCulture2 = CultureInfo.CurrentCulture;
                        object[] objArray4 = new object[] { num4 };
                        empty1 = string.Concat(empty1, string.Format(currentCulture2, "new scale: {0} ", objArray4));
                    }
                    if (dTSExternalMetadataColumn100.DataType != dataType)
                    {
                        CultureInfo cultureInfo2 = CultureInfo.CurrentCulture;
                        object[] str1 = new object[] { dataType.ToString() };
                        empty1 = string.Concat(empty1, string.Format(cultureInfo2, "new data type: {0} ", str1));
                    }
                    if (empty1 != string.Empty)
                    {
                        if (hashtables1.ContainsKey(externalMetadataColumnCollection[l].ID))
                        {
                            Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
                            object[] identificationString = new object[] { dTSExternalMetadataColumn100.IdentificationString, empty1 };
                            errorSupport2.FireErrorWithArgs(-2145348947, out flag3, identificationString);
                            metadataTableByCommand.Dispose();
                            return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                        }
                        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport3 = base.ErrorSupport;
                        object[] identificationString1 = new object[] { dTSExternalMetadataColumn100.IdentificationString, empty1 };
                        errorSupport3.FireWarningWithArgs(-2145348947, identificationString1);
                    }
                }
                else
                {
                    if (hashtables1.ContainsKey(externalMetadataColumnCollection[l].ID))
                    {
                        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport4 = base.ErrorSupport;
                        object[] identificationString2 = new object[] { dTSExternalMetadataColumn100.IdentificationString };
                        errorSupport4.FireErrorWithArgs(-1071610794, out flag2, identificationString2);
                        metadataTableByCommand.Dispose();
                        return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                    }
                    base.ErrorSupport.FireWarningWithArgs(-2145348946, new object[] { dTSExternalMetadataColumn100.IdentificationString });
                }
            }
            for (int m = 0; m < count; m++)
            {
                if (!flagArray[m])
                {
                    Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport5 = base.ErrorSupport;
                    object[] item1 = new object[] { metadataTableByCommand.Rows[m]["ColumnName"] };
                    errorSupport5.FireWarningWithArgs(-2145348945, item1);
                }
            }
            metadataTableByCommand.Dispose();
            hashtables.Clear();
            hashtables1.Clear();
            return DTSValidationStatus.VS_ISVALID;
        }

        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlRoot(Namespace = "http://www.microsoft.com/SqlServer/Dts/ProviderDescriptors.xsd", IsNullable = false)]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptors
        {
            private MyADONetDestination.ProviderDescriptorsProviderDescriptor[] itemsField;

            [XmlElement("ProviderDescriptor")]
            public MyADONetDestination.ProviderDescriptorsProviderDescriptor[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            public ProviderDescriptors()
            {
            }
        }

        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptorsProviderDescriptor
        {
            private MyADONetDestination.ProviderDescriptorsProviderDescriptorSchemaNames schemaNamesField;

            private MyADONetDestination.ProviderDescriptorsProviderDescriptorTableSchemaAttributes tableSchemaAttributesField;

            private MyADONetDestination.ProviderDescriptorsProviderDescriptorColumnSchemaAttributes columnSchemaAttributesField;

            private MyADONetDestination.ProviderDescriptorsProviderDescriptorLiterals literalsField;

            private string sourceTypeField;

            public MyADONetDestination.ProviderDescriptorsProviderDescriptorColumnSchemaAttributes ColumnSchemaAttributes
            {
                get
                {
                    return this.columnSchemaAttributesField;
                }
                set
                {
                    this.columnSchemaAttributesField = value;
                }
            }

            public MyADONetDestination.ProviderDescriptorsProviderDescriptorLiterals Literals
            {
                get
                {
                    return this.literalsField;
                }
                set
                {
                    this.literalsField = value;
                }
            }

            public MyADONetDestination.ProviderDescriptorsProviderDescriptorSchemaNames SchemaNames
            {
                get
                {
                    return this.schemaNamesField;
                }
                set
                {
                    this.schemaNamesField = value;
                }
            }

            [XmlAttribute]
            public string SourceType
            {
                get
                {
                    return this.sourceTypeField;
                }
                set
                {
                    this.sourceTypeField = value;
                }
            }

            public MyADONetDestination.ProviderDescriptorsProviderDescriptorTableSchemaAttributes TableSchemaAttributes
            {
                get
                {
                    return this.tableSchemaAttributesField;
                }
                set
                {
                    this.tableSchemaAttributesField = value;
                }
            }

            public ProviderDescriptorsProviderDescriptor()
            {
            }
        }

        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptorsProviderDescriptorColumnSchemaAttributes
        {
            private string nameColumnNameField;

            private string ordinalPositionColumnNameField;

            private string dataTypeColumnNameField;

            private string maximumLengthColumnNameField;

            private string numericPrecisionColumnNameField;

            private string datetimePrecisionColumnNameField;

            private string numericScaleColumnNameField;

            private string nullableColumnNameField;

            private int numberOfColumnRestrictionsField;

            private bool numberOfColumnRestrictionsFieldSpecified;

            [XmlAttribute]
            public string DataTypeColumnName
            {
                get
                {
                    return this.dataTypeColumnNameField;
                }
                set
                {
                    this.dataTypeColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string DateTimePrecisionColumnName
            {
                get
                {
                    return this.datetimePrecisionColumnNameField;
                }
                set
                {
                    this.datetimePrecisionColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string MaximumLengthColumnName
            {
                get
                {
                    return this.maximumLengthColumnNameField;
                }
                set
                {
                    this.maximumLengthColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string NameColumnName
            {
                get
                {
                    return this.nameColumnNameField;
                }
                set
                {
                    this.nameColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string NullableColumnName
            {
                get
                {
                    return this.nullableColumnNameField;
                }
                set
                {
                    this.nullableColumnNameField = value;
                }
            }

            [XmlAttribute]
            public int NumberOfColumnRestrictions
            {
                get
                {
                    return this.numberOfColumnRestrictionsField;
                }
                set
                {
                    this.numberOfColumnRestrictionsField = value;
                }
            }

            [XmlIgnore]
            public bool NumberOfColumnRestrictionsSpecified
            {
                get
                {
                    return this.numberOfColumnRestrictionsFieldSpecified;
                }
                set
                {
                    this.numberOfColumnRestrictionsFieldSpecified = value;
                }
            }

            [XmlAttribute]
            public string NumericPrecisionColumnName
            {
                get
                {
                    return this.numericPrecisionColumnNameField;
                }
                set
                {
                    this.numericPrecisionColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string NumericScaleColumnName
            {
                get
                {
                    return this.numericScaleColumnNameField;
                }
                set
                {
                    this.numericScaleColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string OrdinalPositionColumnName
            {
                get
                {
                    return this.ordinalPositionColumnNameField;
                }
                set
                {
                    this.ordinalPositionColumnNameField = value;
                }
            }

            public ProviderDescriptorsProviderDescriptorColumnSchemaAttributes()
            {
            }
        }

        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptorsProviderDescriptorLiterals
        {
            private string prefixQualifierField;

            private string suffixQualifierField;

            private string catalogSeparatorField;

            private string schemaSeparatorField;

            [XmlAttribute]
            public string CatalogSeparator
            {
                get
                {
                    return this.catalogSeparatorField;
                }
                set
                {
                    this.catalogSeparatorField = value;
                }
            }

            [XmlAttribute]
            public string PrefixQualifier
            {
                get
                {
                    return this.prefixQualifierField;
                }
                set
                {
                    this.prefixQualifierField = value;
                }
            }

            [XmlAttribute]
            public string SchemaSeparator
            {
                get
                {
                    return this.schemaSeparatorField;
                }
                set
                {
                    this.schemaSeparatorField = value;
                }
            }

            [XmlAttribute]
            public string SuffixQualifier
            {
                get
                {
                    return this.suffixQualifierField;
                }
                set
                {
                    this.suffixQualifierField = value;
                }
            }

            public ProviderDescriptorsProviderDescriptorLiterals()
            {
            }
        }

        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptorsProviderDescriptorSchemaNames
        {
            private string tablesSchemaNameField;

            private string columnsSchemaNameField;

            private string viewsSchemaNameField;

            [XmlAttribute]
            public string ColumnsSchemaName
            {
                get
                {
                    return this.columnsSchemaNameField;
                }
                set
                {
                    this.columnsSchemaNameField = value;
                }
            }

            [XmlAttribute]
            public string TablesSchemaName
            {
                get
                {
                    return this.tablesSchemaNameField;
                }
                set
                {
                    this.tablesSchemaNameField = value;
                }
            }

            [XmlAttribute]
            public string ViewsSchemaName
            {
                get
                {
                    return this.viewsSchemaNameField;
                }
                set
                {
                    this.viewsSchemaNameField = value;
                }
            }

            public ProviderDescriptorsProviderDescriptorSchemaNames()
            {
            }
        }


        [DebuggerStepThrough]
        [DesignerCategory("code")]
        [GeneratedCode("xsd", "2.0.50727.42")]
        [Serializable]
        [XmlType(AnonymousType = true)]
        public class ProviderDescriptorsProviderDescriptorTableSchemaAttributes
        {
            private string tableCatalogColumnNameField;

            private string tableSchemaColumnNameField;

            private string tableNameColumnNameField;

            private string tableTypeColumnNameField;

            private string tableDescriptorField;

            private string viewDescriptorField;

            private string synonymDescriptorField;

            private int numberOfTableRestrictionsField;

            private bool numberOfTableRestrictionsFieldSpecified;

            [XmlAttribute]
            public int NumberOfTableRestrictions
            {
                get
                {
                    return this.numberOfTableRestrictionsField;
                }
                set
                {
                    this.numberOfTableRestrictionsField = value;
                }
            }

            [XmlIgnore]
            public bool NumberOfTableRestrictionsSpecified
            {
                get
                {
                    return this.numberOfTableRestrictionsFieldSpecified;
                }
                set
                {
                    this.numberOfTableRestrictionsFieldSpecified = value;
                }
            }

            [XmlAttribute]
            public string SynonymDescriptor
            {
                get
                {
                    return this.synonymDescriptorField;
                }
                set
                {
                    this.synonymDescriptorField = value;
                }
            }

            [XmlAttribute]
            public string TableCatalogColumnName
            {
                get
                {
                    return this.tableCatalogColumnNameField;
                }
                set
                {
                    this.tableCatalogColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string TableDescriptor
            {
                get
                {
                    return this.tableDescriptorField;
                }
                set
                {
                    this.tableDescriptorField = value;
                }
            }

            [XmlAttribute]
            public string TableNameColumnName
            {
                get
                {
                    return this.tableNameColumnNameField;
                }
                set
                {
                    this.tableNameColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string TableSchemaColumnName
            {
                get
                {
                    return this.tableSchemaColumnNameField;
                }
                set
                {
                    this.tableSchemaColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string TableTypeColumnName
            {
                get
                {
                    return this.tableTypeColumnNameField;
                }
                set
                {
                    this.tableTypeColumnNameField = value;
                }
            }

            [XmlAttribute]
            public string ViewDescriptor
            {
                get
                {
                    return this.viewDescriptorField;
                }
                set
                {
                    this.viewDescriptorField = value;
                }
            }

            public ProviderDescriptorsProviderDescriptorTableSchemaAttributes()
            {
            }
        }

        public class ProviderDescriptorTable
        {
            private const string OleDbProviderName = "System.Data.OleDb.OleDbConnection";

            private Dictionary<string, MyADONetDestination.ProviderDescriptorsProviderDescriptor> descriptorsDict;

            public static MyADONetDestination.ProviderDescriptorsProviderDescriptor DefaultOleDbDescriptor
            {
                get
                {
                    MyADONetDestination.ProviderDescriptorsProviderDescriptor providerDescriptorsProviderDescriptor = new MyADONetDestination.ProviderDescriptorsProviderDescriptor()
                    {
                        SourceType = "System.Data.OleDb.OleDbConnection",
                        SchemaNames = new MyADONetDestination.ProviderDescriptorsProviderDescriptorSchemaNames()
                        {
                            TablesSchemaName = "Tables",
                            ViewsSchemaName = "Views",
                            ColumnsSchemaName = "Columns"
                        },
                        TableSchemaAttributes = new MyADONetDestination.ProviderDescriptorsProviderDescriptorTableSchemaAttributes()
                        {
                            TableCatalogColumnName = "TABLE_CATALOG",
                            TableSchemaColumnName = "TABLE_SCHEMA",
                            TableNameColumnName = "TABLE_NAME",
                            TableTypeColumnName = "TABLE_TYPE",
                            TableDescriptor = "TABLE",
                            ViewDescriptor = "VIEW",
                            SynonymDescriptor = "SYNONYM",
                            NumberOfTableRestrictions = 4
                        },
                        ColumnSchemaAttributes = new MyADONetDestination.ProviderDescriptorsProviderDescriptorColumnSchemaAttributes()
                        {
                            NameColumnName = "COLUMN_NAME",
                            OrdinalPositionColumnName = "ORDINAL_POSITION",
                            DataTypeColumnName = "DATA_TYPE",
                            MaximumLengthColumnName = "CHARACTER_MAXIMUM_LENGTH",
                            NumericPrecisionColumnName = "NUMERIC_PRECISION",
                            NumericScaleColumnName = "NUMERIC_SCALE",
                            NullableColumnName = "IS_NULLABLE",
                            NumberOfColumnRestrictions = 4
                        },
                        Literals = new MyADONetDestination.ProviderDescriptorsProviderDescriptorLiterals()
                        {
                            PrefixQualifier = "\"",
                            SuffixQualifier = "\"",
                            CatalogSeparator = ".",
                            SchemaSeparator = "."
                        }
                    };
                    return providerDescriptorsProviderDescriptor;
                }
            }

            public ProviderDescriptorTable()
            {
                this.Load();
            }

            public bool Contains(string providerName)
            {
                if (providerName == null)
                {
                    return false;
                }
                return this.descriptorsDict.ContainsKey(providerName);
            }

            public MyADONetDestination.ProviderDescriptorsProviderDescriptor GetDescriptor(string providerName)
            {
                if (this.Contains(providerName))
                {
                    return this.descriptorsDict[providerName];
                }
                if (!string.IsNullOrEmpty(providerName) && providerName.Equals("System.Data.OleDb.OleDbConnection", StringComparison.OrdinalIgnoreCase))
                {
                    return MyADONetDestination.ProviderDescriptorTable.DefaultOleDbDescriptor;
                }
                return null;
            }

            private void Load()
            {
                string str;
                using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\110\\SSIS\\Setup\\DTSPath"))
                {
                    str = string.Concat(registryKey.GetValue(string.Empty) as string, "ProviderDescriptors\\");
                }
                if (!Directory.Exists(str))
                {
                    throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadProviderInfos_DirectoryDoesntExist);
                }
                string[] files = Directory.GetFiles(str, "*.xml");
                for (int i = 0; i < (int)files.Length; i++)
                {
                    string str1 = files[i];
                    if (!File.Exists(str1))
                    {
                        throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadProviderInfos_FileMissing);
                    }
                    MyADONetDestination.ProviderDescriptorsProviderDescriptor[] items = MyADONetDestination.ProviderDescriptorTable.LoadDescriptorsFromFile(str1).Items;
                    for (int j = 0; j < (int)items.Length; j++)
                    {
                        MyADONetDestination.ProviderDescriptorsProviderDescriptor providerDescriptorsProviderDescriptor = items[j];
                        this.descriptorsDict[providerDescriptorsProviderDescriptor.SourceType] = providerDescriptorsProviderDescriptor;
                    }
                }
            }

            private static MyADONetDestination.ProviderDescriptors LoadDescriptorsFromFile(string fileName)
            {
                Stream manifestResourceStream = null;
                try
                {
                    manifestResourceStream = typeof(MyADONetDestination.ProviderDescriptors).Assembly.GetManifestResourceStream("Microsoft.SqlServer.Dts.Pipeline.ProviderDescriptors.xsd");
                }
                catch (Exception exception)
                {
                    string message = exception.Message;
                }
                XmlSerializer xmlSerializer = null;
                FileStream fileStream = null;
                XmlValidatingReader xmlValidatingReader = null;
                MyADONetDestination.ProviderDescriptors providerDescriptor = null;
                try
                {
                    try
                    {
                        xmlSerializer = new XmlSerializer(typeof(MyADONetDestination.ProviderDescriptors));
                        fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        xmlValidatingReader = new XmlValidatingReader(fileStream, XmlNodeType.Document, null);
                        xmlValidatingReader.Schemas.Add(XmlSchema.Read(manifestResourceStream, new ValidationEventHandler(MyADONetDestination.ProviderDescriptorTable.XmlReaderValidationEvent)));
                        providerDescriptor = xmlSerializer.Deserialize(xmlValidatingReader) as MyADONetDestination.ProviderDescriptors;
                    }
                    catch (Exception exception2)
                    {
                        Exception exception1 = exception2;
                        if (xmlSerializer != null)
                        {
                            if (fileStream != null)
                            {
                                if (xmlValidatingReader != null)
                                {
                                    if (xmlValidatingReader.Schemas.Count != 0)
                                    {
                                        throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadProviderInfos_UnableToDeserializeFile);
                                    }
                                    throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadMappingFile_UnableToReadSchema);
                                }
                                throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadMappingFile_UnableToCreateValidatingReader);
                            }
                            throw new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadMappingFile_UnableToCreateFileStreamForOpen);
                        }
                        Exception applicationException = new ApplicationException(OakGov.Etl.ZumoDestination.Localized.UnableToLoadMappingFile_UnableToCreateSerializer);
                        throw applicationException;
                    }
                }
                finally
                {
                    if (xmlValidatingReader != null && xmlValidatingReader.ReadState != ReadState.Initial)
                    {
                        xmlValidatingReader.Close();
                    }
                    if (fileStream != null)
                    {
                        fileStream.Close();
                    }
                }
                return providerDescriptor;
            }

            private static void XmlReaderValidationEvent(object sender, ValidationEventArgs args)
            {
                throw args.Exception;
            }
        }


        public class QuoteUtil
        {
            private const string TABLENAMELVL3 = "TABLENAMELVL3";

            private const string TABLENAMELVL2 = "TABLENAMELVL2";

            private const string TABLENAMELVL1 = "TABLENAMELVL1";

            private string prefix;

            private string suffix;

            private string separator;

            private string escapedPrefix;

            private string escapedSuffix;

            private string escapedSeparator;

            private string SimpleWordID;

            private string SpecialWordID_DoubleQuotes;

            private string SpecialWordID_HardBrackets;

            private string SpeicalWordID;

            public string Prefix
            {
                get
                {
                    return this.prefix;
                }
            }

            public string Sufix
            {
                get
                {
                    return this.suffix;
                }
            }

            public QuoteUtil(DbConnection connection)
            {
                this.prefix = "\"";
                this.suffix = "\"";
                this.separator = ".";
                OleDbConnection oleDbConnection = connection as OleDbConnection;
                if (oleDbConnection != null)
                {
                    string connectionString = connection.ConnectionString;
                    string empty = string.Empty;
                    string[] strArrays = connectionString.Split(new char[] { ';' });
                    int num = 0;
                    while (num < (int)strArrays.Length)
                    {
                        string str = strArrays[num];
                        if (!str.Trim().StartsWith("Provider", StringComparison.OrdinalIgnoreCase) || str.IndexOf("SQLOLEDB", StringComparison.OrdinalIgnoreCase) == -1 && str.IndexOf("SQLNCLI", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            num++;
                        }
                        else
                        {
                            this.prefix = "[";
                            this.suffix = "]";
                            this.escapedPrefix = "\\[";
                            this.escapedSuffix = "\\]";
                            this.escapedSeparator = "\\.";
                            this.SpeicalWordID = this.SpecialWordID_HardBrackets;
                            return;
                        }
                    }
                    DataTable oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(OleDbSchemaGuid.DbInfoLiterals, null);
                    if (oleDbSchemaTable != null)
                    {
                        DataRow[] dataRowArray = oleDbSchemaTable.Select("LiteralName = 'Quote_Prefix'");
                        if ((int)dataRowArray.Length <= 0)
                        {
                            this.prefix = string.Empty;
                        }
                        else
                        {
                            this.prefix = dataRowArray[0]["LiteralValue"].ToString();
                            if (this.prefix == null)
                            {
                                this.prefix = string.Empty;
                            }
                        }
                        dataRowArray = oleDbSchemaTable.Select("LiteralName = 'Quote_Suffix'");
                        if ((int)dataRowArray.Length <= 0)
                        {
                            this.suffix = string.Empty;
                        }
                        else
                        {
                            this.suffix = dataRowArray[0]["LiteralValue"].ToString();
                            if (this.suffix == null)
                            {
                                this.suffix = string.Empty;
                            }
                        }
                        dataRowArray = oleDbSchemaTable.Select("LiteralName = 'Schema_Separator'");
                        if ((int)dataRowArray.Length <= 0)
                        {
                            this.separator = string.Empty;
                        }
                        else
                        {
                            this.separator = dataRowArray[0]["LiteralValue"].ToString();
                            if (this.separator == null)
                            {
                                this.separator = string.Empty;
                            }
                        }
                    }
                }
                this.escapedPrefix = this.prefix;
                this.escapedSuffix = this.suffix;
                this.escapedSeparator = this.separator;
                if (this.prefix != "[")
                {
                    this.SpeicalWordID = this.SpecialWordID_DoubleQuotes;
                }
                else
                {
                    this.escapedPrefix = "\\[";
                    this.escapedSuffix = "\\]";
                    this.SpeicalWordID = this.SpecialWordID_HardBrackets;
                }
                if (this.separator == ".")
                {
                    this.escapedSeparator = "\\.";
                }
            }

            public bool GetValidTableName(string tableName, out string tableNameLvl3, out string tableNameLvl2, out string tableNameLvl1)
            {
                tableNameLvl3 = null;
                tableNameLvl2 = null;
                tableNameLvl1 = null;
                string[] strArrays = new string[] { "(", this.TagSimplePattern(this.SimpleWordID, "TABLENAMELVL3"), "|", this.TagSpecialPattern(this.SpeicalWordID, "TABLENAMELVL3"), ")" };
                string str = string.Concat(strArrays);
                string[] strArrays1 = new string[] { "(", this.TagSimplePattern(this.SimpleWordID, "TABLENAMELVL2"), "|", this.TagSpecialPattern(this.SpeicalWordID, "TABLENAMELVL2"), ")" };
                string str1 = string.Concat(strArrays1);
                string[] strArrays2 = new string[] { "(", this.TagSimplePattern(this.SimpleWordID, "TABLENAMELVL1"), "|", this.TagSpecialPattern(this.SpeicalWordID, "TABLENAMELVL1"), ")" };
                string str2 = string.Concat(strArrays2);
                string str3 = null;
                string[] strArrays3 = new string[] { "\\s*", str1, this.escapedSeparator, str2, "\\s*" };
                str3 = string.Concat(strArrays3);
                Match match = Regex.Match(tableName, str3, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (match.Success && match.Length == tableName.Length)
                {
                    tableNameLvl2 = match.Groups["TABLENAMELVL2"].Value;
                    tableNameLvl1 = match.Groups["TABLENAMELVL1"].Value;
                    return true;
                }
                str3 = string.Concat("\\s*", str2, "\\s*");
                match = Regex.Match(tableName, str3, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (match.Success && match.Length == tableName.Length)
                {
                    tableNameLvl1 = match.Groups["TABLENAMELVL1"].Value;
                    return true;
                }
                string[] strArrays4 = new string[] { "\\s*", str, this.escapedSeparator, str1, this.escapedSeparator, str2, "\\s*" };
                str3 = string.Concat(strArrays4);
                match = Regex.Match(tableName, str3, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (!match.Success || match.Length != tableName.Length)
                {
                    return false;
                }
                tableNameLvl3 = match.Groups["TABLENAMELVL3"].Value;
                tableNameLvl2 = match.Groups["TABLENAMELVL2"].Value;
                tableNameLvl1 = match.Groups["TABLENAMELVL1"].Value;
                return true;
            }

            public string QuoteName(string name)
            {
                if (name.StartsWith(this.prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(this.suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
                if (this.suffix.CompareTo("]") == 0)
                {
                    return string.Concat(this.prefix, name.Replace("]", "]]"), "]");
                }
                if (this.suffix.CompareTo("\"") != 0)
                {
                    return string.Concat(this.prefix, name, this.suffix);
                }
                return string.Concat(this.prefix, name.Replace("\"", "\"\""), this.suffix);
            }

            private string TagSimplePattern(string SimpleWordID, string tag)
            {
                string[] strArrays = new string[] { "(?<", tag, ">", SimpleWordID, ")" };
                return string.Concat(strArrays);
            }

            private string TagSpecialPattern(string SpeicalWordID, string tag)
            {
                string[] strArrays = new string[] { this.escapedPrefix, "(?<", tag, ">", SpeicalWordID, ")", this.escapedSuffix };
                return string.Concat(strArrays);
            }
        }
    }
}
