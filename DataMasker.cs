using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OakGov.Etl.ZumoDestination
{
    [ComVisible(false)]
    [DtsPipelineComponent(DisplayName = "Data Masker",IconResource="OakGov.Etl.ZumoDestination.ADONETDest.ico")]
    public class DataMasker : PipelineComponent
    {
        int[] inputBufferColumnIndex, outputBufferColumnIndex;

        public override void ProvideComponentProperties()
        {
            // Set Component information
            ComponentMetaData.Name = "Data Masker";
            ComponentMetaData.Description = "A SSIS Data Flow Transformation Component to Provide Basic Data Masking";
            ComponentMetaData.ContactInfo = "Arthur Zubarev";

            // Reset the component
            base.RemoveAllInputsOutputsAndCustomProperties();

            // add input objects
            IDTSInput100 input = ComponentMetaData.InputCollection.New();
            input.Name = "Input";
            input.Description = "Contains un-masked columns.";
           
            // add output objects
            IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
            output.Name = "Output";
            output.Description = "Contains masked columns. gets set automatically.";
            output.SynchronousInputID = input.ID; // Synchronus transformation

            // Add error objects
            IDTSOutput100 errorOutput = ComponentMetaData.OutputCollection.New();
            errorOutput.Name = "Error";
            errorOutput.IsErrorOut = true;
        }

        public override DTSValidationStatus Validate()
        {
            // Determine whether the metadata needs refresh
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();

            bool cancel = false;

            foreach (IDTSInputColumn100 column in input.InputColumnCollection)
            {
                try
                {
                    IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(column.LineageID);
                }
                catch (Exception)
                {
                    var msg = string.Format("The input column {0} does not match a coulumn in the upstream component.", column.IdentificationString);
                    ComponentMetaData.FireError(0, ComponentMetaData.Name, msg, "", 0, out cancel);
                    return DTSValidationStatus.VS_NEEDSNEWMETADATA;
                }
            }

            // validate input to be of type string/numeric only
            bool pbCancel = false;
            for (int x = 0; x < input.InputColumnCollection.Count; x++)
            {
                if (!(input.InputColumnCollection[x].DataType == DataType.DT_STR ||
                    input.InputColumnCollection[x].DataType == DataType.DT_WSTR ||
                    input.InputColumnCollection[x].DataType == DataType.DT_DECIMAL ||
                    input.InputColumnCollection[x].DataType == DataType.DT_NUMERIC ||
                    input.InputColumnCollection[x].DataType == DataType.DT_TEXT))
                {
                    var msg = string.Format("Column {0} cannot be used for data masking.\nThe data typoe supplied was {1}.\nThe Supported data types are DT_STR, DT_WSTR, DT_DECIMAL, DT_NUMERIC and DT_TEXT.\nUnmark the offending column(s) to correct.",input.InputColumnCollection[x].Name,input.InputColumnCollection[x].DataType.ToString());
                    ComponentMetaData.FireError(0, ComponentMetaData.Name, msg, "", 0,out pbCancel);
                    return DTSValidationStatus.VS_ISBROKEN;
                }
            }

            // create corresponding output columns dynamically
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];

            foreach (IDTSInputColumn100 inputColumn in input.InputColumnCollection)
            {
                bool isExist = false;
                foreach (IDTSOutputColumn100 outputColumn in output.OutputColumnCollection)
                {
                    if(outputColumn.Name == string.Format("Masked_{0}",inputColumn.Name))
                    {
                        isExist = true;
                    }
                }

                if (!isExist)
                {
                    IDTSOutputColumn100 outputCol = output.OutputColumnCollection.New();
                    outputCol.Name = string.Format("Masked_{0}", inputColumn.Name);
                    outputCol.Description = string.Format("Masked {0}", inputColumn.Name);
                    outputCol.SetDataTypeProperties(inputColumn.DataType,inputColumn.Length,inputColumn.Precision,inputColumn.Scale,inputColumn.CodePage);
                }
            }

            // remove redundant output columns that don't match the input columns
            if (output.OutputColumnCollection.Count > input.InputColumnCollection.Count)
            {
                foreach (IDTSOutputColumn100 outputColumn in output.OutputColumnCollection)
                {
                    bool isRedundant = true;
                    foreach (IDTSInputColumn100 inputColumn in input.InputColumnCollection)
                    {
                        isRedundant = outputColumn.Name.Contains(string.Format("Masked_{0}", inputColumn.Name)) ? false : true;
                        if (!isRedundant)
                        {
                            break;
                        }
                        if (isRedundant)
                        {
                            output.OutputColumnCollection.RemoveObjectByID(outputColumn.ID);
                        }
                    }
                }
            }
            return DTSValidationStatus.VS_ISVALID;
        }

        public override void PreExecute()
        {
            //base.PreExecute();
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            inputBufferColumnIndex = new int[input.InputColumnCollection.Count];

            for (int x = 0; x < input.InputColumnCollection.Count; x++)
            {
                IDTSInputColumn100 column = input.InputColumnCollection[x];
                inputBufferColumnIndex[x] = BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID);
            }

            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];
            outputBufferColumnIndex = new int[output.OutputColumnCollection.Count];

            for (int x = 0; x < output.OutputColumnCollection.Count; x++)
            {
                IDTSOutputColumn100 outcol = output.OutputColumnCollection[x];
                // A synchronous output does not appear in output buffer, but in input buffer
                outputBufferColumnIndex[x] = BufferManager.FindColumnByLineageID(input.Buffer, outcol.LineageID);
            }
        }

        // The actual data masking
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            if (!buffer.EndOfRowset)
            {
                while (buffer.NextRow())
                {
                    for (int x = 0; x < inputBufferColumnIndex.Length; x++)
                    {
                        DataType BufferColDataType;
                        BufferColDataType = buffer.GetColumnInfo(inputBufferColumnIndex[x]).DataType;
                        if (!buffer.IsNull(x))
                        {
                            buffer.SetString(outputBufferColumnIndex[x], MaskData(buffer.GetString(inputBufferColumnIndex[x])));
                        }
                    }
                }
            }
        }

        // Provides a basic data masking with scrambling column content
        private string MaskData(string InputData)
        {
            string MaskedData = InputData;
            if (MaskedData.Length > 0)
            {
                // The technigue used to mask the data is to replace numbers with random numbers and letters with letters.
                char[] chars = new char[InputData.Length];
                Random rand = new Random(DateTime.Now.Millisecond);
                int index = 0;

                while (InputData.Length > 0)
                {
                    // Get a random number between 0 and the length of the word.
                    int next = rand.Next(0, InputData.Length - 1);
                    
                    // take the character from the random position and add to our char array.
                    chars[index] = InputData[next];

                    // Remove the character from the word.
                    InputData = InputData.Substring(0, next) + InputData.Substring(next + 1);
                    ++index;
                }
                MaskedData = new String(chars);
            }

            // Scrambled or empty
            return MaskedData;
        }
    }
}
