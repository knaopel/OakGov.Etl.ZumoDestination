using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using System.Globalization;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Data;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using OakGov.Etl.ZumoDestination.Models;
using OakGov.Etl.ZumoDestination.clmjmsweb1p;
using System.Text;
using System.Xml.Linq;

namespace OakGov.Etl.ZumoDestination
{
    //[DtsPipelineComponent(DisplayName = "BLOB Extractor Destination", Description = "Writes values of BLOB columns to files")]
    //public class BlobDst : PipelineComponent
    //{
    //    string m_DestDir;
    //    int m_FileNameColumnIndex = -1;
    //    int m_BlobColumnIndex = -1;
        
    //    public override void ProvideComponentProperties()
    //    {
    //        IDTSInput100 input = ComponentMetaData.InputCollection.New();
    //        input.Name = "BLOB Extractor Destination Input";
    //        input.HasSideEffects = true;

    //        IDTSRuntimeConnection100 conn = ComponentMetaData.RuntimeConnectionCollection.New();
    //        conn.Name = "FileConnection";
    //    }

    //    public override void AcquireConnections(object transaction)
    //    {
    //        IDTSRuntimeConnection100 conn = ComponentMetaData.RuntimeConnectionCollection[0];
    //        //m_DestDir = (string)conn.ConnectionManager.AquireConnection(null);
    //        m_DestDir = "D:\\MyFiles";

    //        if (m_DestDir.Length > 0 && m_DestDir[m_DestDir.Length - 1] != '\\')
    //            m_DestDir += "\\";
    //    }

    //    public override IDTSInputColumn100 SetUsageType(int inputID, IDTSVirtualInput100 virtualInput, int lineageID, DTSUsageType usageType)
    //    {
    //        IDTSInputColumn100 inputColumn = base.SetUsageType(inputID, virtualInput, lineageID, usageType);
    //        IDTSCustomProperty100 customProp;

    //        customProp = inputColumn.CustomPropertyCollection.New();
    //        customProp.Name = "IsFileName";
    //        customProp.Value = (object)false;

    //        customProp = inputColumn.CustomPropertyCollection.New();
    //        customProp.Name = "IsBLOB";
    //        customProp.Value = (object)false;

    //        return inputColumn;
    //    }

    //    public override void PreExecute()
    //    {
    //        IDTSInput100 input = ComponentMetaData.InputCollection[0];
    //        IDTSInputColumnCollection100 inputColumns = input.InputColumnCollection;
    //        IDTSCustomProperty100 customProp;

    //        foreach (IDTSInputColumn100 column in inputColumns)
    //        {
    //            customProp = column.CustomPropertyCollection["IsFileName"];
    //            if ((bool)customProp.Value == true)
    //            {
    //                m_FileNameColumnIndex = (int)BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID);
    //            }

    //            customProp = column.CustomPropertyCollection["IsBLOB"];
    //            if ((bool)customProp.Value == true)
    //            {
    //                m_BlobColumnIndex = (int)BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID);
    //            }
    //        }
    //    }

    //    public override void ProcessInput(int inputID, PipelineBuffer buffer)
    //    {
    //        while (buffer.NextRow())
    //        {
    //            string strFileName = buffer.GetString(m_FileNameColumnIndex);
    //            int blobLength = (int)buffer.GetBlobLength(m_BlobColumnIndex);
    //            byte[] blobData = buffer.GetBlobData(m_BlobColumnIndex, 0, blobLength);

    //            strFileName = TranslateFileName(strFileName);
                
    //            // Make sure directory exists before creating file
    //            FileInfo fi = new FileInfo(strFileName);
    //            if (!fi.Directory.Exists)
    //                fi.Directory.Create();

    //            // Write the data to the file
    //            FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write, FileShare.None);
    //            fs.Write(blobData, 0, blobLength);
    //            fs.Close();
    //        }
    //    }

    //    private string TranslateFileName(string fileName)
    //    {
    //        if (fileName.Length > 2 && fileName[1] == ':')
    //            return m_DestDir + fileName.Substring(3, fileName.Length - 3);
    //        else
    //            return m_DestDir + fileName;
    //    }
    //}
    [DtsPipelineComponent(DisplayName = "ZUMO Destination", ComponentType = ComponentType.DestinationAdapter, IconResource = "OakGov.Etl.ZumoDestination.ZumoDest.ico")]
    public class ZumoDestination : PipelineComponent
    {
        private const string ADMINKEY = "AdminKey";
        private const string APPID = "AppId";
        private const string ZUMONAME = "AzureMobileServicesName";
        private const string CONNNAME = "ZUMOHttp";

        bool pbCancel = false;
        string urlFormat = "https://{0}.azure-mobile.net";
        private string adminKey, appId, zumoName, tableName;
        private Type listType;
        bool flag, flag1, flag2, flag3, flag4, flag5;

        private List<Inmate> inmatesServiceList, inmatesToSave;
        private List<Charge> chargesServiceList, chargesToSave;
        private List<Hold> holdsServiceList, holdsToSave;
        //private DataTable dataTable, zumoTable;
        private DataColumn[] tableColumns;
        private int[] bufferIdxs;
        private BufferNameMap columnDictionary;

        int adds = 0;
        int updates = 0;

        #region Overrides
        public override void ProvideComponentProperties()
        {
            // reset the component
            IDTSRuntimeConnection100 item;
            base.ProvideComponentProperties();
            //base.RemoveAllInputsOutputsAndCustomProperties();
            //base.ComponentMetaData.Name = "ZUMO Destination";
            //base.ComponentMetaData.Description = "Managed Connection Manager";
            //IDTSComponentMetaData100 componentMetaData = base.ComponentMetaData;
            //string[] description = new string[] { base.ComponentMetaData.Description, ";Oakland County, Michigan;", " © ", "Oakland County, Michigan; All rights reserved.", null };
            //int componentVersion = this.GetComponentVersion();
            //description[4] = componentVersion.ToString(CultureInfo.InvariantCulture);
            //componentMetaData.ContactInfo = string.Concat(description);
            //try
            //{
            //    item = base.ComponentMetaData.RuntimeConnectionCollection[CONNNAME];
            //}
            //catch (Exception exception)
            //{
            //    item = base.ComponentMetaData.RuntimeConnectionCollection.New();
            //    item.Name = CONNNAME;
            //    item.Description = "Connection to ZUMO";
            //}
            ////ComponentMetaData.RuntimeConnectionCollection.RemoveAll();

            //IDTSInput100 inputName = ComponentMetaData.InputCollection.New();
            //inputName.Name = "Input";
            //inputName.HasSideEffects = true;
            //inputName.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;

            //IDTSOutput100 errorOutputName = base.ComponentMetaData.OutputCollection.New();
            //errorOutputName.Name = "ZUMO Destination Error Output";
            //errorOutputName.IsErrorOut = true;
            //errorOutputName.SynchronousInputID = inputName.ID;
            //errorOutputName.ExclusionGroup = 1;

            //base.ComponentMetaData.UsesDispositions = true;
            //inputName.ExternalMetadataColumnCollection.IsUsed = true;
            //base.ComponentMetaData.ValidateExternalMetadata = true;
            //base.ComponentMetaData.Version = this.GetComponentVersion();

            IDTSCustomProperty100 tableNameDescription = base.ComponentMetaData.CustomPropertyCollection.New();
            tableNameDescription.Name = "TableName";
            tableNameDescription.ExpressionType = DTSCustomPropertyExpressionType.CPET_NOTIFY;
            tableNameDescription.Description = "Table Prop Description";
            tableNameDescription.Value = string.Empty;
        }

        //public override void AcquireConnections(object transaction)
        //{
        //    IDTSRuntimeConnection100 item;
        //    object obj;
        //    bool flag, flag1, flag2, flag3;
        //    try
        //    {
        //        item = base.ComponentMetaData.RuntimeConnectionCollection[CONNNAME];
        //    }
        //    catch (Exception ex)
        //    {
        //        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport = base.ErrorSupport;
        //        object[] objArray = new object[] { CONNNAME };
        //        errorSupport.FireErrorWithArgs(-1071611878, out flag1, objArray);
        //        throw new PipelineComponentHResultException(-1071611878);
        //    }
        //    IDTSConnectionManager100 connectionManager = item.ConnectionManager;
        //    if (connectionManager == null)
        //    {
        //        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport1 = base.ErrorSupport;
        //        object[] objArray1 = new object[] { CONNNAME };
        //        errorSupport1.FireErrorWithArgs(-1071611851, out flag2, objArray1);
        //        throw new PipelineComponentHResultException(-1071611851);
        //    }
        //    try
        //    {
        //        obj = connectionManager.AcquireConnection(transaction);
        //    }
        //    catch (Exception ex2)
        //    {
        //        Exception ex1 = ex2;
        //        var errorSupport2 = base.ErrorSupport;
        //        object[] connectionManagerId = new object[] { item.ConnectionManagerID, ex1.Message };
        //        errorSupport2.FireErrorWithArgs(-1071610798, out flag3, connectionManagerId);
        //        throw new PipelineComponentHResultException(ex1.Message, -1071610798);
        //    }
        //    var httpConnection = obj as HttpClientConnection100;

        //    //base.AcquireConnections(transaction);
        //}

        public override void PreExecute()
        {
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            bufferIdxs = new int[input.InputColumnCollection.Count];
            columnDictionary = new BufferNameMap();

            for (int x = 0; x < input.InputColumnCollection.Count; x++)
            {
                IDTSInputColumn100 column = input.InputColumnCollection[x];
                bufferIdxs[x] = BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID);
                columnDictionary.Add(column.Name, bufferIdxs[x]);
            }



            //base.PreExecute();
            IDTSVariables100 variables = null;
            VariableDispenser.LockForRead(ADMINKEY);
            VariableDispenser.LockForRead(APPID);
            VariableDispenser.LockForRead(ZUMONAME);
            VariableDispenser.GetVariables(out variables);
            object adminKeyObj = variables[0].Value;
            object appIdObj = variables[1].Value;
            object zumoNameObj = variables[2].Value;
            this.adminKey = adminKeyObj.ToString();
            this.appId = appIdObj.ToString();
            this.zumoName = zumoNameObj.ToString();

            //this.dataTable = new DataTable()
            //{
            //    Locale = CultureInfo.InvariantCulture
            //};
            //IDTSInput100 dTSInput = base.ComponentMetaData.InputCollection[0];
            //IDTSExternalMetadataColumnCollection100 externalColumnCollection = dTSInput.ExternalMetadataColumnCollection;
            //IDTSInputColumnCollection100 inputColumnCollection = dTSInput.InputColumnCollection;
            //int count = inputColumnCollection.Count;
            //this.tableColumns = new DataColumn[count];
            //IDTSExternalMetadataColumn100[] externalMetadataColumnArray = new IDTSExternalMetadataColumn100[count];
            //this.bufferIdxs = new int[count];
            //for (int i = 0; i < count; i++)
            //{
            //    IDTSInputColumn100 inputColumn = inputColumnCollection[i];
            //    IDTSExternalMetadataColumn100 externalMetadataColumn = externalColumnCollection.FindObjectByID(inputColumn.ExternalMetadataColumnID);
            //    externalMetadataColumnArray[i] = externalMetadataColumn;
            //    DataType dataType = inputColumn.DataType;
            //    bool flag5 = false;
            //    dataType = PipelineComponent.ConvertBufferDataTypeToFitManaged(dataType, ref flag5);
            //    Type dataRecordType = PipelineComponent.BufferTypeToDataRecordType(dataType);
            //    this.tableColumns[i] = new DataColumn(externalMetadataColumn.Name, dataRecordType);
            //    int lineageId = inputColumn.LineageID;
            //    try
            //    {
            //        this.bufferIdxs[i] = base.BufferManager.FindColumnByLineageID(dTSInput.Buffer, lineageId);
            //    }
            //    catch (Exception ex)
            //    {
            //        Microsoft.SqlServer.Dts.ManagedMsg.ErrorSupport errorSupport2 = base.ErrorSupport;
            //        object[] name = new object[] { lineageId, externalMetadataColumn.Name };
            //        errorSupport2.FireErrorWithArgs(-1071610795, out flag4, name);
            //        throw new PipelineComponentHResultException(-1071610795);
            //    }
            //}
            //this.dataTable.Columns.AddRange(this.tableColumns);

            IDTSCustomProperty100 item = base.ComponentMetaData.CustomPropertyCollection["TableName"];
            string str;
            if (item.Value == null)
            {
                str = null;
            }
            else
            {
                str = item.Value.ToString().Trim();
            }
            this.tableName = str;
            switch (this.tableName)
            {
                case "charges":
                    chargesServiceList = GetAllCharges();
                    ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Got {0} charges from the online service.", chargesServiceList.Count), "", 0, ref pbCancel); 
                    chargesToSave = new List<Charge>();
                    break;
                case "holds":
                    holdsServiceList = GetAllHolds();
                    ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Got {0} holds from the online service.", holdsServiceList.Count), "", 0, ref pbCancel); 
                    holdsToSave = new List<Hold>();
                    break;
                case "inmates":
                default:
                    try
                    {
                        inmatesServiceList = GetAllInmates();
                    }
                    catch (Exception)
                    {
                        RetryGetAllInmates();
                    }
                    ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Got {0} inmates from the online service.", inmatesServiceList.Count), "", 0, ref pbCancel); 
                    inmatesToSave = new List<Inmate>();
                    break;
            }
        }

        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            //int count = this
            while (buffer.NextRow())
            {
                switch (tableName)
                {
                    case "charges":
                        var chargeRow = new ChargeBuffer(buffer, columnDictionary);
                        if (chargesServiceList.Exists(c => c.id.Equals(chargeRow.CHARGENBR)))
                        {
                            // get existing charge
                            var charge = chargesServiceList.SingleOrDefault(c => c.id.Equals(chargeRow.CHARGENBR));
                            var needsUpdate = false;

                            if (chargeRow.INMATEID != charge.inmateId)
                            {
                                needsUpdate = true;
                                charge.inmateId = chargeRow.INMATEID;
                            }
                            if (!chargeRow.BONDAMT_IsNull || (charge.bondAmount != null)) // only have to act if one has a value, if both are null we don't need to act
                            {
                                if (chargeRow.BONDAMT_IsNull && (charge.bondAmount != null)) // row is null, charge has value
                                {
                                    // update to null
                                    needsUpdate = true;
                                    charge.bondAmount = null;
                                }
                                else if (!chargeRow.BONDAMT_IsNull)
                                {
                                    if (chargeRow.BONDAMT != charge.bondAmount) // row has a value and they do not match
                                    {
                                        // update to value
                                        needsUpdate = true;
                                        charge.bondAmount = chargeRow.BONDAMT;
                                    }
                                    else if (charge.bondAmount == null) // row has a value and charge is null
                                    {
                                        needsUpdate = true;
                                        charge.bondAmount = chargeRow.BONDAMT;
                                    }
                                }
                            }
                            if (chargeRow.BONDTYPE != charge.bondType)
                            {
                                needsUpdate = true;
                                charge.bondType = chargeRow.BONDTYPE;
                            }
                            if (chargeRow.BONDDESC != charge.bondDescription)
                            {
                                needsUpdate = true;
                                charge.bondDescription = chargeRow.BONDDESC;
                            }
                            if (chargeRow.COURTDESC != charge.court)
                            {
                                needsUpdate = true;
                                charge.court = chargeRow.COURTDESC;
                            }
                            if (chargeRow.MICRDESC.Replace('¿', ' ') != charge.charge)
                            {
                                needsUpdate = true;
                                charge.charge = chargeRow.MICRDESC.Replace('¿', ' ');
                            }

                            if (needsUpdate)
                            {
                                // update Charge
                                var chargeBody = Serialize(charge);
                                UpdateItem(chargeBody, charge.id);
                                updates++;
                            }
                            chargesToSave.Add(charge);
                        }
                        else
                        {
                            // insert new charge
                            var newCharge = new Charge(chargeRow);
                            var newChargeBody = Serialize(newCharge);
                            InsertItem(newChargeBody, newCharge.id);
                            adds++;
                            chargesToSave.Add(newCharge);
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Created Charge {0}", newCharge.id), "", 0, ref pbCancel);
                        }
                        break;
                    case "holds":
                        var holdRow = new HoldBuffer(buffer, columnDictionary);
                        if (holdsServiceList.Exists(h => h.id.Equals(string.Format("{0}-{1}", holdRow.ID, holdRow.WARRANTNUM))))
                        {
                            var hold = holdsServiceList.SingleOrDefault(h => h.id.Equals(string.Format("{0}-{1}", holdRow.ID, holdRow.WARRANTNUM)));
                            var isUpdated = false;

                            if (hold.bondType != holdRow.BONDTYPE)
                            {
                                isUpdated = true;
                                hold.bondType = holdRow.BONDTYPE;
                            }
                            if (hold.charge != holdRow.CHARGE)
                            {
                                isUpdated = true;
                                hold.charge = holdRow.CHARGE;
                            }
                            if (hold.comment != holdRow.Comment)
                            {
                                isUpdated = true;
                                hold.comment = holdRow.Comment;
                            }
                            if (hold.holdAgency != holdRow.HOLDAGENCY)
                            {
                                isUpdated = true;
                                hold.holdAgency = holdRow.HOLDAGENCY;
                            }
                            if (!holdRow.BONDAMOUNT_IsNull || hold.bondAmount != null) // at least one has a value
                            {
                                if (holdRow.BONDAMOUNT_IsNull && (hold.bondAmount != null)) // row is null, hold has a value
                                {
                                    // set hold to null
                                    isUpdated = true;
                                    hold.bondAmount = null;
                                }
                                else if (!holdRow.BONDAMOUNT_IsNull && (holdRow.BONDAMOUNT != hold.bondAmount)) // row has a value and they are not equal
                                {
                                    isUpdated = true;
                                    hold.bondAmount = holdRow.BONDAMOUNT;
                                }
                            }

                            if (isUpdated)
                            {
                                // update Hold
                                var holdBody = Serialize(hold);
                                UpdateItem(holdBody, hold.id);
                                updates++;
                                ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Updated Hold {0}", hold.id), "", 0, ref pbCancel);
                            }
                            holdsToSave.Add(hold);
                        }
                        else
                        {
                            // insert hold
                            var newHold = new Hold(holdRow);
                            var newHoldBody = Serialize(newHold);
                            InsertItem(newHoldBody, newHold.id);
                            adds++;
                            holdsToSave.Add(newHold);
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Created Hold {0}", newHold.id), "", 0, ref pbCancel);
                        }
                        break;
                    case "inmates":
                    default:
                        var inmateRow = new InmateBuffer(buffer, columnDictionary);
                        if (inmatesServiceList.Exists(i => i.id.Equals(inmateRow.INMATEID)))
                        {
                            List<string> updateStr = new List<string>();
                            Inmate item = inmatesServiceList.SingleOrDefault(i => i.id.Equals(inmateRow.INMATEID));
                            var isUpdated = false;
                            if (item.lastName != inmateRow.LASTNM)
                            {
                                isUpdated = true;
                                item.lastName = inmateRow.LASTNM;
                            }
                            if (item.firstName != inmateRow.FIRSTNM)
                            {
                                isUpdated = true;
                                updateStr.Add("FirstName");
                                item.firstName = inmateRow.FIRSTNM;
                            }
                            if (item.middleName != inmateRow.MIDDLENM)
                            {
                                isUpdated = true;
                                updateStr.Add("MiddleName");
                                item.middleName = inmateRow.MIDDLENM;
                            }
                            if (item.bookedDate != inmateRow.BOOKDT)
                            {
                                isUpdated = true;
                                updateStr.Add("BookDt");
                                item.bookedDate = inmateRow.BOOKDT;
                                item.booked = inmateRow.BOOKED;
                            }

                            if (item.bookingId != inmateRow.BOOKNBR)
                            {
                                isUpdated = true;
                                updateStr.Add("BookNbr");
                                item.bookingId = inmateRow.BOOKNBR;
                            }

                            // set or update inmate image
                            var webSvcImage = GetInmateImage(inmateRow.INMATEID, inmateRow.BOOKNBR);
                            if (item.inmateImage != webSvcImage)
                            {
                                isUpdated = true;
                                updateStr.Add("InmateImage");
                                item.inmateImage = webSvcImage;
                            }
                            // TODO: get Image from Service
                            item.birthDate = ReconcileDates(inmateRow, "DOB", item.birthDate, ref isUpdated);
                            item.releaseDate = ReconcileDates(inmateRow, "PHYSRLSDT", item.releaseDate, ref isUpdated);
                            item.released = ReconcileDates(inmateRow, "RELEASED", item.released, ref isUpdated);

                            if (item.gender != inmateRow.GENDERCD)
                            {
                                isUpdated = true;
                                updateStr.Add("GenderCd");
                                item.gender = inmateRow.GENDERCD;
                            }
                            if (item.jailLocation != inmateRow.FACILITYDESC)
                            {
                                isUpdated = true;
                                updateStr.Add("FacilityDesc");
                                item.jailLocation = inmateRow.FACILITYDESC;
                            }
                            if (!(inmateRow.ACTIVEHOLDS_IsNull && item.activeHolds == null)) // both have values
                            {
                                if (inmateRow.ACTIVEHOLDS_IsNull && item.activeHolds != null) // row is null and item has value
                                {
                                    isUpdated = true;
                                    updateStr.Add("ActiveHolds");
                                    item.activeHolds = null;
                                }
                                else if (!inmateRow.ACTIVEHOLDS_IsNull && (item.activeHolds != inmateRow.ACTIVEHOLDS)) // row is not null and values not the same
                                {
                                    isUpdated = true;
                                    updateStr.Add("ActiveHolds");
                                    item.activeHolds = inmateRow.ACTIVEHOLDS;
                                }
                            }
                            if (isUpdated)
                            {
                                // TODO: update item on service
                                var inmateBody = Serialize(item);
                                UpdateItem(inmateBody, item.id);
                                updates++;
                                var updated = string.Join(", ", updateStr.ToArray());
                                ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Updated Inmate {0} with {1}", item.id, updated), "", 0, ref pbCancel);
                            }
                            inmatesToSave.Add(item);
                        }
                        else
                        {
                            // insert item
                            var newInmate = new Inmate(inmateRow);
                            newInmate.inmateImage = GetInmateImage(inmateRow.INMATEID, inmateRow.BOOKNBR);
                            var newInmateBody = Serialize(newInmate);
                            InsertItem(newInmateBody, newInmate.id);
                            adds++;
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Created Inmate {0}", inmateRow.INMATEID), "", 0, ref pbCancel);
                            inmatesToSave.Add(newInmate);
                        }
                        break;
                }
            }
        }

        public override void PostExecute()
        {
            base.PostExecute();
            int deletes = 0;
            switch (tableName)
            {
                case "charges":
                    var chargesToRemove = chargesServiceList.Except(chargesToSave).ToList();
                    if (chargesToRemove.Count > 0)
                    {
                        var ids = string.Join(",", chargesToRemove.Select(c => c.id).ToArray());
                        DeleteBatch batch = new DeleteBatch() { ids = ids, tableName = tableName };
                        if (DeleteItems(batch))
                        {
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Charge(s) deleted with id(s) {0}.", ids), "", 0, ref pbCancel);
                        }
                        else
                        {
                            ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Charge(s) NOT deleted with id(s) {0}.", ids), "", 0, out pbCancel);
                        }
                    }
                    else
                    {
                        ComponentMetaData.FireInformation(0, ComponentMetaData.Name, "No Charges to delete.", "", 0, ref pbCancel);
                    }
                    break;
                case "holds":
                    var holdsToRemove = holdsServiceList.Except(holdsToSave).ToList();
                    if (holdsToRemove.Count > 0)
                    {
                        var ids = string.Join(",", holdsToRemove.Select(h => h.id).ToArray());
                        DeleteBatch batch = new DeleteBatch() { ids = ids, tableName = tableName };
                        if (DeleteItems(batch))
                        {
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Hold(s) deleted with id(s) {0}.", ids), "", 0, ref pbCancel);
                        }
                        else
                        {
                            ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Hold(s) NOT deleted with id(s) {0}.", ids), "", 0, out pbCancel);
                        }
                    }
                    else
                    {
                        ComponentMetaData.FireInformation(0, ComponentMetaData.Name, "No Holds to delete.", "", 0, ref pbCancel);
                    }
                    break;
                case "inmates":
                default:
                    var inmatesToRemove = inmatesServiceList.Except(inmatesToSave).ToList();
                    if (inmatesToRemove.Count > 0)
                    {
                        var ids = string.Join(",", inmatesToRemove.Select(i => i.id).ToArray());
                        DeleteBatch batch = new DeleteBatch() { ids = ids, tableName = tableName };
                        if (DeleteItems(batch))
                        {
                            ComponentMetaData.FireInformation(0, ComponentMetaData.Name, string.Format("Items(s) {0} deleted from {1}.", ids, tableName), string.Empty, 0, ref pbCancel);
                        }
                        else
                        {
                            ComponentMetaData.FireError(0, ComponentMetaData.Name, string.Format("Items(s) {0}  NOT deleted from {1}.", ids, tableName), string.Empty, 0, out pbCancel);

                        }
                    }
                    else
                    {
                        ComponentMetaData.FireInformation(0, ComponentMetaData.Name, "No Inmates to delete.", "", 0, ref pbCancel);
                    }
                    deletes = inmatesToRemove.Count;
                    break;
            }
            InsertSync(new SyncItem() { adds = adds, updates = updates, deletes = deletes, syncType = tableName, syncTime = DateTime.Now });

        }

        
        public override DTSValidationStatus Validate()
        {
        //    IDTSInput100 input = ComponentMetaData.InputCollection[0];
        //    IDTSVirtualInput100 vInput = input.GetVirtualInput();

        //    bool cancel = false;

        //    foreach (IDTSInputColumn100 column in input.InputColumnCollection)
        //    {
        //        try
        //        {
        //            IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(column.LineageID);
        //        }
        //        catch (Exception)
        //        {
        //            var msg = string.Format("The input column {0} does not match a column in the upstream component.", column.IdentificationString);
        //            ComponentMetaData.FireError(0, ComponentMetaData.Name, msg, "", 0, out cancel);
        //            return DTSValidationStatus.VS_NEEDSNEWMETADATA;
        //        }
        //    }
        //    return DTSValidationStatus.VS_ISVALID;
            return base.Validate();
        }
        #endregion

        private string GetAllResp()
        {
               var getUrl = string.Format(this.urlFormat + "/api/getall/{1}", this.zumoName, this.tableName);
               return AzureWebServicesConnection.Get(new Uri(getUrl), this.adminKey, true);
        }

        List<Inmate> GetAllInmates()
        {
            string resp = GetAllResp();
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = Int32.MaxValue;
                return serializer.Deserialize<List<Inmate>>(resp);
            }
            catch (Exception e)
            {
                throw new ArgumentNullException("There was a problem with the serialization.", e);
            }
        }

        List<Charge> GetAllCharges(int RetryCount = 0)
        {
            if (RetryCount < 5)
            {
                try
                {
                    var resp = GetAllResp();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = Int32.MaxValue;
                    return serializer.Deserialize<List<Charge>>(resp);
                }
                catch (Exception)
                {
                    RetryCount++;
                    ComponentMetaData.FireInformation(0, ComponentMetaData.Name, "Waiting to retry call for all charges.", "", 0, ref pbCancel);
                    Thread.Sleep(1000);
                    return GetAllCharges(RetryCount);
                }
            }
            else
            {
                ComponentMetaData.FireError(0, ComponentMetaData.Name, "Unable to get all charges.", "", 0, out pbCancel);
                return new List<Charge>();
            }
        }

        List<Hold> GetAllHolds(int RetryCount = 0)
        {
            if (RetryCount < 5)
            {
                try
                {
                    string resp = GetAllResp();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = Int32.MaxValue;
                    return serializer.Deserialize<List<Hold>>(resp);
                }
                catch (Exception)
                {
                    RetryCount++;
                    ComponentMetaData.FireInformation(0, ComponentMetaData.Name, "Waiting to rety call for all holds.", "", 0, ref pbCancel);
                    Thread.Sleep(1000);
                    return GetAllHolds(RetryCount);
                }
            }
            else
            {
                ComponentMetaData.FireError(0, ComponentMetaData.Name, "Unable to get all holds.", "", 0, out pbCancel);
                return new List<Hold>();
            }
        }

        private DateTime? ReconcileDates(InmateBuffer Row, string RowDateName, DateTime? ItemDate, ref bool IsUpdated)
        {
            DateTime? rowDate = new DateTime?();
            bool hasDate;

            switch (RowDateName)
            {
                case "DOB":
                    hasDate = !Row.DOB_IsNull;
                    if (hasDate)
                        rowDate = new DateTime?(Row.DOB);
                    break;
                case "PHYSRLSDT":
                    hasDate = !Row.PHYSRLSDT_IsNull;
                    if (hasDate)
                        rowDate = new DateTime?(Row.PHYSRLSDT);
                    break;
                case "RELEASED":
                    hasDate = !Row.RELEASED_IsNull;
                    if (hasDate)
                        rowDate = new DateTime?(Row.RELEASED);
                    break;
                case "BOOKDT":
                    hasDate = !Row.BOOKDT_IsNull;
                    if (hasDate)
                        rowDate = new DateTime?(Row.BOOKDT);
                    break;
                default:
                    hasDate = !Row.BOOKED_IsNull;
                    if (hasDate)
                        rowDate = new DateTime?(Row.BOOKED);
                    break;
            }
            if (hasDate)
            {
                // row date has a value
                if (rowDate != ItemDate)
                {
                    // dates are different
                    IsUpdated = true;
                }
                return rowDate;
            }
            else
            {
                // row date is null
                if (ItemDate.HasValue)
                {
                    // item date had a value. Set IsUpdated to true
                    IsUpdated = true;
                }
                // item date is also null - no change
                return null;
            }
        }
        private void RetryGetAllInmates(int retryCount = 0)
        {
            if (retryCount < 5)
            {
                this.ComponentMetaData.FireInformation(0, "GetAllInmates", "Waiting to retry call for all items", "", 0, ref pbCancel);
                Thread.Sleep(1000);
                try
                {
                    this.inmatesServiceList = GetAllInmates();
                }
                catch (Exception)
                {
                    retryCount++;
                    RetryGetAllInmates(retryCount);
                }
            }
            else
            {
                this.ComponentMetaData.FireError(0, "GetAllInmates", "Unable to get all items from service.", string.Empty, 0, out pbCancel);
            }
        }

        private string GetInmateImage(string InmateId, string BookNumber)
        {
            ImageService imageService = new ImageService();
            // get image

            // Web service requires 7 digits - zero padded on front
            int noOfCharToPrefix = 0;
            StringBuilder inmateIdToSend = new StringBuilder();
            string tempImageCompare;
            if (InmateId.Length < 7)
            {
                // how many chars are there?
                noOfCharToPrefix = 7 - InmateId.Length;
                for (int i = 0; i < noOfCharToPrefix; i++)
                {
                    inmateIdToSend.Append("0");
                }
                inmateIdToSend.Append(InmateId);
            }

            // F is front-facing image, P is profile image
            tempImageCompare = imageService.GetInmateImage(inmateIdToSend.ToString(), BookNumber, "F");
            XDocument doc = XDocument.Parse(tempImageCompare);
            string frontPhotoElement = string.Empty;
            try
            {
                frontPhotoElement = doc.Element("InmateRecord").Element("InmatePhoto").Descendants().First().Value.ToString();
            }
            catch (Exception) { }

            return string.IsNullOrEmpty(frontPhotoElement) ? frontPhotoElement : string.Format("data:image/jpeg;base64,{0}", frontPhotoElement);
        }

        private void InsertItem(string itemBody, string id, int RetryCount = 0)
        {
            try
            {
                string insertUrl = string.Format(urlFormat + "/tables/{1}", zumoName, tableName);
                var insertUri = new Uri(insertUrl);
                var result = AzureWebServicesConnection.InsertItem(insertUri, itemBody, adminKey);
                if (result.Contains("Error"))
                {
                    throw new Exception(result);
                }
            }
            catch (Exception ex)
            {
                ComponentMetaData.FireInformation(978, ComponentMetaData.Name, ex.Message, "", 0, ref pbCancel);
                if (RetryCount < 5)
                {
                    ComponentMetaData.FireInformation(979, ComponentMetaData.Name, string.Format("Waiting to insert into {0} table, id: {1}", tableName, id), "", 0, ref pbCancel);
                    Thread.Sleep(1000);
                    RetryCount++;
                    InsertItem(itemBody, id, RetryCount);
                }
                else
                {
                    ComponentMetaData.FireInformation(980, ComponentMetaData.Name, string.Format("Unable to insert into {0} table, id: {1} to service.", tableName, id), "", 0, ref pbCancel);

                }
            }
        }

        private bool DeleteItems(DeleteBatch Batch)
        {
            var url = string.Format(urlFormat + "/api/batchdelete", zumoName);
            var batchUri = new Uri(url);
            var body = Serialize(Batch);
            return AzureWebServicesConnection.DeleteBatch(batchUri, body, adminKey);
        }

        private void InsertSync(SyncItem syncItem)
        {
            var insertUrl = string.Format(urlFormat + "/tables/syncLog", zumoName);
            var insertUri = new Uri(insertUrl);
            var insertBody = Serialize(syncItem);
            var result = AzureWebServicesConnection.InsertItem(insertUri, insertBody, adminKey);
        }
        
        private string Serialize(object item)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = Int32.MaxValue;
            return ser.Serialize(item);
        }

        private void UpdateItem(string ItemBody,string Id, int RetryCount = 0)
        {
            try
            {
                string updateUrl = string.Format(this.urlFormat + "/tables/{1}/{2}", this.zumoName, this.tableName, Id);
                string result = AzureWebServicesConnection.UpdateItem(new Uri(updateUrl), ItemBody, this.adminKey);
                if (result.Contains("Error"))
                {
                    throw new Exception(result);
                }
            }
            catch (Exception ex)
            {
                this.ComponentMetaData.FireInformation(978, "UpdateItem", ex.Message, string.Empty, 0, ref pbCancel);
                if (RetryCount < 5)
                {
                    var msg = string.Format("Waiting to retry call to update {0}: {1}", this.tableName, Id);
                    this.ComponentMetaData.FireInformation(979, "UpdateItem", msg, string.Empty, 0, ref pbCancel);
                    Thread.Sleep(1000);
                    RetryCount++;
                    UpdateItem(ItemBody, Id, RetryCount);
                }
                else
                {
                    var msg = string.Format("Unable to update item: {0}, {1}", this.tableName, Id);
                    this.ComponentMetaData.FireError(980, "UpdateItem", msg, string.Empty, 0, out pbCancel);
                }
            }
        }

        private void SendDataToDestination(PipelineBuffer buffer, int bufferIndex)
        {
        }
        private int GetComponentVersion()
        {
            DtsPipelineComponentAttribute customAttribute = (DtsPipelineComponentAttribute)Attribute.GetCustomAttribute(base.GetType(), typeof(DtsPipelineComponentAttribute), false);
            return customAttribute.CurrentVersion;
        }
    }
}
