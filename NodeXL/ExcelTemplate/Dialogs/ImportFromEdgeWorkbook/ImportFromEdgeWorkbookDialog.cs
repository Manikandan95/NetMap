

//  Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using Microsoft.Research.CommunityTechnologies.AppLib;
using System.Diagnostics;

namespace Microsoft.NodeXL.ExcelTemplate
{
//*****************************************************************************
//  Class: ImportFromEdgeWorkbookDialog
//
/// <summary>
/// Imports two edge columns from another open workbook to the edge worksheet.
/// </summary>
///
/// <remarks>
/// All the importation work is done by this dialog.  The caller need only pass
/// a workbook to the constructor and call <see cref="Form.ShowDialog()" />.
/// </remarks>
//*****************************************************************************

public partial class ImportFromEdgeWorkbookDialog : ExcelTemplateForm
{
    //*************************************************************************
    //  Constructor: ImportFromEdgeWorkbookDialog()
    //
    /// <overloads>
    /// Initializes a new instance of the <see
    /// cref="ImportFromEdgeWorkbookDialog" /> class.
    /// </overloads>
    ///
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="ImportFromEdgeWorkbookDialog" /> class with a workbook.
    /// </summary>
    ///
    /// <param name="destinationNodeXLWorkbook">
    /// Workbook to which the edge workbook will be imported.
    /// </param>
    ///
    /// <param name="clearDestinationTablesFirst">
    /// true if the NodeXL tables in <paramref
    /// name="destinationNodeXLWorkbook" /> should be cleared first.
    /// </param>
    //*************************************************************************

    public ImportFromEdgeWorkbookDialog
    (
        Microsoft.Office.Interop.Excel.Workbook destinationNodeXLWorkbook,
        Boolean clearDestinationTablesFirst
    )
    : this()
    {
        // Instantiate an object that saves and retrieves the user settings for
        // this dialog.  Note that the object automatically saves the settings
        // when the form closes.

        m_oImportFromEdgeWorkbookDialogUserSettings =
            new ImportFromEdgeWorkbookDialogUserSettings(this);

        m_oDestinationNodeXLWorkbook = destinationNodeXLWorkbook;
        m_bClearDestinationTablesFirst = clearDestinationTablesFirst;

        lbxSourceWorkbook.PopulateWithOtherWorkbookNames(
            m_oDestinationNodeXLWorkbook);

        DoDataExchange(false);

        AssertValid();
    }

    //*************************************************************************
    //  Constructor: ImportFromEdgeWorkbookDialog()
    //
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="ImportFromEdgeWorkbookDialog" /> class for the Visual Studio
    /// designer.
    /// </summary>
    ///
    /// <remarks>
    /// Do not use this constructor.  It is for use by the Visual Studio
    /// designer only.
    /// </remarks>
    //*************************************************************************

    public ImportFromEdgeWorkbookDialog()
    {
        InitializeComponent();

        // AssertValid();
    }

    //*************************************************************************
    //  Method: PopulateSourceColumnsCheckedListBox()
    //
    /// <summary>
    /// Populates the clbSourceColumns CheckedListBox.
    /// </summary>
    //*************************************************************************

    protected void
    PopulateSourceColumnsCheckedListBox()
    {
        AssertValid();

        System.Windows.Forms.ListBox.ObjectCollection oItems =
            clbSourceColumns.Items;

        oItems.Clear();

        // Attempt to get the non-empty range of the active worksheet of the
        // selected source workbook.

        Range oNonEmptyRange;

        if ( !TryGetSourceWorkbookNonEmptyRange(out oNonEmptyRange) )
        {
            return;
        }

        Boolean bSourceColumnsHaveHeaders =
            cbxSourceColumnsHaveHeaders.Checked;

        // Get the first row and column of the non-empty range.

        Range oFirstRow = oNonEmptyRange.get_Resize(1, Missing.Value);
        Range oColumn = oNonEmptyRange.get_Resize(Missing.Value, 1);

        Object [,] oFirstRowValues = ExcelUtil.GetRangeValues(oFirstRow);

        // Loop through the columns.

        Int32 iNonEmptyColumns = oNonEmptyRange.Columns.Count;
        Int32 iColumnOneBased = oColumn.Column;

        for (Int32 i = 1; i <= iNonEmptyColumns; i++, iColumnOneBased++)
        {
            String sColumnLetter = ExcelUtil.GetColumnLetter(
                ExcelUtil.GetRangeAddress( (Range)oColumn.Cells[1, 1] ) );

            // Get the value of the column's first cell, if there is one.

            String sFirstCellValue;

            if ( !ExcelUtil.TryGetNonEmptyStringFromCell(oFirstRowValues, 1,
                i, out sFirstCellValue) )
            {
                sFirstCellValue = null;
            }

            String sItemText = GetSourceColumnItemText(sFirstCellValue,
                sColumnLetter, bSourceColumnsHaveHeaders);

            oItems.Add( new ObjectWithText(iColumnOneBased, sItemText) );

            // Move to the next column.

            oColumn = oColumn.get_Offset(0, 1);
        }
    }

    //*************************************************************************
    //  Method: GetSourceColumnItemText()
    //
    /// <summary>
    /// Gets the text to use for an item in the clbSourceColumns
    /// CheckedListBox.
    /// </summary>
    ///
    /// <param name="sFirstSourceCellValue">
    /// String value from the first cell in the source column, or null if the
    /// first cell doesn't contain a string.
    /// </param>
    ///
    /// <param name="sColumnLetter">
    /// Excel's letter for the source column.
    /// </param>
    ///
    /// <param name="bSourceColumnsHaveHeaders">
    /// true if the source columns have headers.
    /// </param>
    ///
    /// <returns>
    /// The text to use.
    /// </returns>
    //*************************************************************************

    protected String
    GetSourceColumnItemText
    (
        String sFirstSourceCellValue,
        String sColumnLetter,
        Boolean bSourceColumnsHaveHeaders
    )
    {
        AssertValid();

        if (sFirstSourceCellValue == null)
        {
            // Just use the column letter.

            return ( String.Format(

                "Column {0}"
                ,
                sColumnLetter
                ) );
        }

        // Truncate the first cell if necessary.

        const Int32 MaxItemTextLength = 30;

        if (sFirstSourceCellValue.Length > MaxItemTextLength)
        {
            sFirstSourceCellValue = sFirstSourceCellValue.Substring(
                0, MaxItemTextLength) + "...";
        }

        if (bSourceColumnsHaveHeaders)
        {
            // The first cell is a header.

            return ( String.Format(

                "\"{0}\"",

                sFirstSourceCellValue
                ) );
        }

        // The first cell isn't a header.  Precede the cell value with the
        // column letter.

        return ( String.Format(

            "Column {0}: \"{1}\""
            ,
            sColumnLetter,
            sFirstSourceCellValue
            ) );
    }

    //*************************************************************************
    //  Method: UpdateVertexComboBox()
    //
    /// <summary>
    /// Updates the vertex 1 or vertex 2 ComboBox when the user checks or
    /// unchecks a source column.
    /// </summary>
    ///
    /// <param name="e">
    /// Event arguments passed to clbSourceColumns_ItemCheck().
    /// </param>
    ///
    /// <param name="cbxVertex">
    /// The vertex 1 or vertex 2 ComboBox.
    /// </param>
    //*************************************************************************

    protected void
    UpdateVertexComboBox
    (
        ItemCheckEventArgs e,
        ComboBox cbxVertex
    )
    {
        AssertValid();

        // Save the ObjectWithText that is selected in the vertex ComboBox, if
        // there is one.

        ObjectWithText oOldSelectedObjectWithText =
            (ObjectWithText)cbxVertex.SelectedItem;

        // Get the ObjectWithText that was checked or unchecked in
        // clbSourceColumns.

        ObjectWithText oCheckedOrUncheckedObjectWithText =
            (ObjectWithText)clbSourceColumns.Items[e.Index];

        if (e.NewValue == CheckState.Checked)
        {
            // Insert it into the vertex ComboBox.

            InsertItemIntoVertexComboBox(oCheckedOrUncheckedObjectWithText,
                cbxVertex);
        }
        else
        {
            // Remove it from the vertex ComboBox.

            if (oCheckedOrUncheckedObjectWithText ==
                oOldSelectedObjectWithText)
            {
                cbxVertex.SelectedIndex = -1;
            }

            cbxVertex.Items.Remove(oCheckedOrUncheckedObjectWithText);
        }
    }

    //*************************************************************************
    //  Method: InsertItemIntoVertexComboBox()
    //
    /// <summary>
    /// Inserts an item into the vertex 1 or vertex 2 ComboBox.
    /// </summary>
    ///
    /// <param name="oItemToInsert">
    /// The item to insert.
    /// </param>
    ///
    /// <param name="cbxVertex">
    /// The vertex 1 or vertex 2 ComboBox.
    /// </param>
    ///
    /// <remarks>
    /// The item is inserted in the column order of the source worksheet.
    /// </remarks>
    //*************************************************************************

    protected void
    InsertItemIntoVertexComboBox
    (
        ObjectWithText oItemToInsert,
        ComboBox cbxVertex
    )
    {
        Debug.Assert(cbxVertex != null);
        AssertValid();

        ComboBox.ObjectCollection oItems = cbxVertex.Items;
        Int32 iItems = oItems.Count;

        Int32 iColumnToInsertOneBased =
            ObjectWithTextToColumnNumberOneBased(oItemToInsert);

        Int32 i;

        for (i = 0; i < iItems; i++)
        {
            Debug.Assert(oItems[i] is ObjectWithText);

            ObjectWithText oItem = (ObjectWithText)oItems[i];

            if ( iColumnToInsertOneBased <
                ObjectWithTextToColumnNumberOneBased(oItem) )
            {
                break;
            }
        }

        oItems.Insert(i, oItemToInsert);
    }

    //*************************************************************************
    //  Method: CheckAllSourceColumns()
    //
    /// <summary>
    /// Checks or unchecks all the source columns.
    /// </summary>
    ///
    /// <param name="bSelect">
    /// true to check, false to uncheck.
    /// </param>
    //*************************************************************************

    protected void
    CheckAllSourceColumns
    (
        Boolean bSelect
    )
    {
        AssertValid();

        System.Windows.Forms.ListBox.ObjectCollection oItems =
            clbSourceColumns.Items;

        Int32 iItems = oItems.Count;

        for (Int32 i = 0; i < iItems; i++)
        {
            clbSourceColumns.SetItemChecked(i, bSelect);
        }
    }

    //*************************************************************************
    //  Method: TryGetSourceWorkbookNonEmptyRange()
    //
    /// <summary>
    /// Attempts to get the non-empty range of the active worksheet of the
    /// selected source workbook.
    /// </summary>
    ///
    /// <param name="oNonEmptyRange">
    /// Where the non-empty range gets stored if true is returned.
    /// </param>
    ///
    /// <returns>
    /// true if successful.
    /// </returns>
    //*************************************************************************

    protected Boolean
    TryGetSourceWorkbookNonEmptyRange
    (
        out Range oNonEmptyRange
    )
    {
        AssertValid();

        oNonEmptyRange = null;

        Debug.Assert(lbxSourceWorkbook.Items.Count > 0);

        String sSourceWorkbookName = (String)lbxSourceWorkbook.SelectedItem;

        Object oSourceWorksheetAsObject;

        try
        {
            oSourceWorksheetAsObject =
                m_oDestinationNodeXLWorkbook.Application.Workbooks[
                    sSourceWorkbookName].ActiveSheet;
        }
        catch (COMException)
        {
            // TODO: This occurred once.

            oSourceWorksheetAsObject = null;
        }

        if ( oSourceWorksheetAsObject == null ||
            !(oSourceWorksheetAsObject is Worksheet) )
        {
            this.ShowWarning( String.Format(

                WorkbookImporterBase.SourceWorkbookSheetIsNotWorksheetMessage
                ,
                sSourceWorkbookName
                ) );

            return (false);
        }

        Worksheet oSourceWorksheet = (Worksheet)oSourceWorksheetAsObject;

        if ( !ExcelUtil.TryGetNonEmptyRangeInWorksheet(oSourceWorksheet,
            out oNonEmptyRange) )
        {
            this.ShowWarning( String.Format(

                "The selected worksheet in {0} is empty.  It has no columns"
                + " that can be imported."
                ,
                sSourceWorkbookName
                ) );

            return (false);
        }

        return (true);
    }

    //*************************************************************************
    //  Method: DoDataExchange()
    //
    /// <summary>
    /// Transfers data between the dialog's fields and its controls.
    /// </summary>
    ///
    /// <param name="bFromControls">
    /// true to transfer data from the dialog's controls to its fields, false
    /// for the other direction.
    /// </param>
    ///
    /// <returns>
    /// true if the transfer was successful.
    /// </returns>
    //*************************************************************************

    protected Boolean
    DoDataExchange
    (
        Boolean bFromControls
    )
    {
        if (bFromControls)
        {
            // Validate the controls.

            if (cbxVertex1.SelectedIndex >= 0 && cbxVertex2.SelectedIndex >= 0
                && cbxVertex1.SelectedItem == cbxVertex2.SelectedItem)
            {
                OnInvalidComboBox(cbxVertex1,
                    "The same imported column can't be used for both Vertex 1"
                    + " and Vertex 2."
                    );

                return (false);
            }

            m_oImportFromEdgeWorkbookDialogUserSettings.
                SourceColumnsHaveHeaders =
                cbxSourceColumnsHaveHeaders.Checked;
        }
        else
        {
            cbxSourceColumnsHaveHeaders.Checked =
                m_oImportFromEdgeWorkbookDialogUserSettings.
                SourceColumnsHaveHeaders;
        }

        EnableControls();

        return (true);
    }

    //*************************************************************************
    //  Method: EnableControls()
    //
    /// <summary>
    /// Enables or disables the dialog's controls.
    /// </summary>
    //*************************************************************************

    protected void
    EnableControls()
    {
        AssertValid();

        // Note: Don't use clbSourceColumns.CheckedItems.Count here, because
        // the ItemCheck event on clbSourceColumns occurs before the
        // CheckedItems collection gets updated.

        pnlVertices.Enabled = (cbxVertex1.Items.Count > 0);

        btnCheckAllSourceColumns.Enabled = btnUncheckAllSourceColumns.Enabled =
            (clbSourceColumns.Items.Count > 0);

        btnOK.Enabled = pnlVertices.Enabled && cbxVertex1.SelectedIndex >= 0
            && cbxVertex2.SelectedIndex >= 0;
    }

    //*************************************************************************
    //  Method: ImportFromEdgeWorkbook()
    //
    /// <summary>
    /// Imports edges from a source workbook to the edge worksheet of the
    /// NodeXL workbook.
    /// </summary>
    ///
    /// <remarks>
    /// This should be called only after DoDataExchange(true) returns true.
    /// </remarks>
    //*************************************************************************

    protected void
    ImportFromEdgeWorkbook()
    {
        AssertValid();

        EdgeWorkbookImporter oEdgeWorkbookImporter =
            new EdgeWorkbookImporter();

        String sSourceWorkbookName = (String)lbxSourceWorkbook.SelectedItem;

        // Get a list of selected source columns.

        LinkedList<Int32> oOneBasedColumnNumbersToImport =
            new LinkedList<Int32>();

        foreach (ObjectWithText oCheckedItem in clbSourceColumns.CheckedItems)
        {
            oOneBasedColumnNumbersToImport.AddLast(
                ObjectWithTextToColumnNumberOneBased(oCheckedItem) );
        }

        Debug.Assert(oOneBasedColumnNumbersToImport.Count > 0);

        Boolean bSourceColumnsHaveHeaders =
            m_oImportFromEdgeWorkbookDialogUserSettings.
            SourceColumnsHaveHeaders;

        // Get the columns to use for vertex 1 and vertex 2.

        Debug.Assert(cbxVertex1.SelectedItem != null);

        Int32 iColumnToUseForVertex1OneBased =
            ObjectWithTextToColumnNumberOneBased(
                (ObjectWithText)cbxVertex1.SelectedItem);

        Debug.Assert(cbxVertex2.SelectedItem != null);

        Int32 iColumnToUseForVertex2OneBased =
            ObjectWithTextToColumnNumberOneBased(
                (ObjectWithText)cbxVertex2.SelectedItem);

        oEdgeWorkbookImporter.ImportEdgeWorkbook(sSourceWorkbookName,
            oOneBasedColumnNumbersToImport, iColumnToUseForVertex1OneBased,
            iColumnToUseForVertex2OneBased, bSourceColumnsHaveHeaders,
            m_bClearDestinationTablesFirst, m_oDestinationNodeXLWorkbook);
    }

    //*************************************************************************
    //  Method: ObjectWithTextToColumnNumberOneBased()
    //
    /// <summary>
    /// Retrieves the one-based column number from an ObjectWithText.
    /// </summary>
    ///
    /// <param name="oObjectWithText">
    /// The ObjectWithText that has a one-based column number in its Object
    /// property.
    /// </param>
    //*************************************************************************

    protected Int32
    ObjectWithTextToColumnNumberOneBased
    (
        ObjectWithText oObjectWithText
    )
    {
        Debug.Assert(oObjectWithText != null);
        AssertValid();

        Debug.Assert(oObjectWithText.Object is Int32);

        return ( (Int32)oObjectWithText.Object );
    }

    //*************************************************************************
    //  Method: OnLoad()
    //
    /// <summary>
    /// Handles the Load event.
    /// </summary>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected override void
    OnLoad
    (
        EventArgs e
    )
    {
        AssertValid();

        base.OnLoad(e);

        switch (lbxSourceWorkbook.Items.Count)
        {
            case 0:

                this.ShowWarning(ExcelWorkbookListBox.NoOtherWorkbooks);
                this.Close();

                break;

            case 1:

                lbxSourceWorkbook.SelectedIndex = 0;

                break;

            default:

                // (Do nothing.)

                break;
        }
    }

    //*************************************************************************
    //  Method: OnSourceWorkbookChanged()
    //
    /// <summary>
    /// Performs tasks necessary when the user selects a source workbook.
    /// </summary>
    //*************************************************************************

    protected void
    OnSourceWorkbookChanged()
    {
        AssertValid();

        cbxVertex1.Items.Clear();
        cbxVertex2.Items.Clear();

        PopulateSourceColumnsCheckedListBox();

        if (clbSourceColumns.Items.Count >= 2)
        {
            // Check and select the first two columns.

            clbSourceColumns.SetItemChecked(0, true);
            clbSourceColumns.SetItemChecked(1, true);

            Debug.Assert(cbxVertex1.Items.Count >= 2);
            Debug.Assert(cbxVertex2.Items.Count >= 2);

            cbxVertex1.SelectedIndex = 0;
            cbxVertex2.SelectedIndex = 1;
        }

        EnableControls();
    }

    //*************************************************************************
    //  Method: lbxSourceWorkbook_SelectedIndexChanged()
    //
    /// <summary>
    /// Handles the SelectedIndexChanged event on the lbxSourceWorkbook
    /// ListBox.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    lbxSourceWorkbook_SelectedIndexChanged
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        OnSourceWorkbookChanged();
    }

    //*************************************************************************
    //  Method: cbxSourceColumnsHaveHeaders_CheckedChanged()
    //
    /// <summary>
    /// Handles the CheckedChanged event on the cbxSourceColumnsHaveHeaders
    /// CheckBox.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    cbxSourceColumnsHaveHeaders_CheckedChanged
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (lbxSourceWorkbook.SelectedItem != null)
        {
            OnSourceWorkbookChanged();
        }
    }

    //*************************************************************************
    //  Method: clbSourceColumns_ItemCheck()
    //
    /// <summary>
    /// Handles the ItemCheck event on the clbSourceColumns CheckedListBox.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    clbSourceColumns_ItemCheck
    (
        object sender,
        ItemCheckEventArgs e
    )
    {
        AssertValid();

        UpdateVertexComboBox(e, cbxVertex1);
        UpdateVertexComboBox(e, cbxVertex2);
        EnableControls();
    }

    //*************************************************************************
    //  Method: cmsSourceColumns_Opening()
    //
    /// <summary>
    /// Handles the Opening event on the cmsSourceColumns ContextMenuStrip.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    cmsSourceColumns_Opening
    (
        object sender,
        System.ComponentModel.CancelEventArgs e
    )
    {
        AssertValid();

        // Don't show the context menu if there are no source columns.

        e.Cancel = (clbSourceColumns.Items.Count == 0);
    }

    //*************************************************************************
    //  Method: btnCheckAllSourceColumns_Click()
    //
    /// <summary>
    /// Handles the Click event on the btnCheckAllSourceColumns button.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    btnCheckAllSourceColumns_Click
    (
        object sender,
        System.EventArgs e
    )
    {
        AssertValid();

        CheckAllSourceColumns(true);
    }

    //*************************************************************************
    //  Method: btnUncheckAllSourceColumns_Click()
    //
    /// <summary>
    /// Handles the Click event on the btnUncheckAllSourceColumns button.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    btnUncheckAllSourceColumns_Click
    (
        object sender,
        System.EventArgs e
    )
    {
        AssertValid();

        CheckAllSourceColumns(false);
    }

    //*************************************************************************
    //  Method: cbxVertex_SelectedIndexChanged()
    //
    /// <summary>
    /// Handles the SelectedIndexChanged event on the cbxVertex1 and cbxVertex2
    /// ComboBox.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    cbxVertex_SelectedIndexChanged
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EnableControls();
    }

    //*************************************************************************
    //  Method: btnOK_Click()
    //
    /// <summary>
    /// Handles the Click event on the btnOK button.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    btnOK_Click
    (
        object sender,
        System.EventArgs e
    )
    {
        if ( !DoDataExchange(true) )
        {
            return;
        }

        try
        {
            ImportFromEdgeWorkbook();
        }
        catch (ImportWorkbookException oImportWorkbookException)
        {
            this.ShowWarning(oImportWorkbookException.Message);
            return;
        }
        catch (Exception oException)
        {
            ErrorUtil.OnException(oException);
            return;
        }

        DialogResult = DialogResult.OK;
        this.Close();
    }


    //*************************************************************************
    //  Method: AssertValid()
    //
    /// <summary>
    /// Asserts if the object is in an invalid state.  Debug-only.
    /// </summary>
    //*************************************************************************

    // [Conditional("DEBUG")]

    public override void
    AssertValid()
    {
        base.AssertValid();

        Debug.Assert(m_oImportFromEdgeWorkbookDialogUserSettings != null);
        Debug.Assert(m_oDestinationNodeXLWorkbook != null);
        // m_bClearDestinationTablesFirst
    }


    //*************************************************************************
    //  Protected fields
    //*************************************************************************

    /// User settings for this dialog.

    protected ImportFromEdgeWorkbookDialogUserSettings
        m_oImportFromEdgeWorkbookDialogUserSettings;

    /// Workbook to which the edge workbook will be imported.

    protected Microsoft.Office.Interop.Excel.Workbook
        m_oDestinationNodeXLWorkbook;

    /// true if the NodeXL tables should be cleared first.

    protected Boolean m_bClearDestinationTablesFirst;
}


//*****************************************************************************
//  Class: ImportFromEdgeWorkbookDialogUserSettings
//
/// <summary>
/// Stores the user's settings for the <see
/// cref="ImportFromEdgeWorkbookDialog" />.
/// </summary>
///
/// <remarks>
/// The user settings include the form size and location.
/// </remarks>
//*****************************************************************************

[ SettingsGroupNameAttribute("ImportFromEdgeWorkbookDialog2") ]

public class ImportFromEdgeWorkbookDialogUserSettings : FormSettings
{
    //*************************************************************************
    //  Constructor: ImportFromEdgeWorkbookDialogUserSettings()
    //
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="ImportFromEdgeWorkbookDialogUserSettings" /> class.
    /// </summary>
    ///
    /// <param name="oForm">
    /// The form to save settings for.
    /// </param>
    //*************************************************************************

    public ImportFromEdgeWorkbookDialogUserSettings
    (
        Form oForm
    )
    : base (oForm, true)
    {
        Debug.Assert(oForm != null);

        // (Do nothing.)

        AssertValid();
    }

    //*************************************************************************
    //  Property: SourceColumnsHaveHeaders
    //
    /// <summary>
    /// Gets or sets a flag that indicating whether the source columns have
    /// headers.
    /// 
    /// </summary>
    ///
    /// <value>
    /// true if the source columns have headers.  The default is false.
    /// </value>
    //*************************************************************************

    [ UserScopedSettingAttribute() ]
    [ DefaultSettingValueAttribute("false") ]

    public Boolean
    SourceColumnsHaveHeaders
    {
        get
        {
            AssertValid();

            return ( (Boolean)this[SourceColumnsHaveHeadersKey] );
        }

        set
        {
            this[SourceColumnsHaveHeadersKey] = value;

            AssertValid();
        }
    }


    //*************************************************************************
    //  Method: AssertValid()
    //
    /// <summary>
    /// Asserts if the object is in an invalid state.  Debug-only.
    /// </summary>
    //*************************************************************************

    // [Conditional("DEBUG")]

    public override void
    AssertValid()
    {
        base.AssertValid();

        // (Do nothing else.)
    }


    //*************************************************************************
    //  Protected constants
    //*************************************************************************

    /// Name of the settings key for the SourceColumnsHaveHeaders property.

    protected const String SourceColumnsHaveHeadersKey =
        "SourceColumnsHaveHeaders";


    //*************************************************************************
    //  Protected fields
    //*************************************************************************

    // (None.)
}
}
