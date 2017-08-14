﻿Imports EditorPlus.Core
Imports EditorPlus.UI
Imports Microsoft.Office.Interop.Excel
Imports Microsoft.Office.Tools.Ribbon
Imports Net.Surviveplus.RegularExpressionQuery
Imports Net.Surviveplus.SakuraMacaron.Core
Imports Net.Surviveplus.SakuraMacaron.OfficeAddIn.Excel
Imports Net.Surviveplus.SakuraMacaron.OfficeAddIn.UI

Public Class EditorPlusRibbon

    Private Sub EditorPlusRibbon_Load(ByVal sender As System.Object, ByVal e As RibbonUIEventArgs) Handles MyBase.Load

    End Sub

    Private insertTextPane As ElementControlPane(Of InsertText)

    Private Sub InsertTextButton_Click(sender As Object, e As RibbonControlEventArgs) Handles InsertTextButton.Click

        If Me.insertTextPane Is Nothing Then

            Dim c = New InsertText
            AddHandler c.InsertButtonClick,
                Sub(sender2, e2)

                    Dim macaron As New ExcelMacaron(ThisAddIn.Current.Application)
                    macaron.InsertText(e2.Text, e2.InsertTo, e2.SkipIfStartedOrEndWithText)
                End Sub

            Me.insertTextPane = New ElementControlPane(Of InsertText)(c)
            Me.insertTextPane.Pane = ThisAddIn.Current.CustomTaskPanes.Add(Me.insertTextPane.Control, "Insert Text", ThisAddIn.Current.Application.ActiveWindow)
            Me.insertTextPane.Pane.Width = 350
        End If

        Me.insertTextPane?.Show()
    End Sub

    Private insertSerialNumberPane As ElementControlPane(Of InsertSerialNumber)

    Private Sub InsertSerialNumberButton_Click(sender As Object, e As RibbonControlEventArgs) Handles InsertSerialNumberButton.Click

        If Me.insertSerialNumberPane Is Nothing Then

            Dim c = New InsertSerialNumber()
            AddHandler c.InsertButtonClick,
                Sub(sender2, e2)

                    Dim macaron As New ExcelMacaron(ThisAddIn.Current.Application)
                    macaron.InsertSerialNumber(e2.StartNumber, e2.InsertTo, e2.Padding, e2.SkipIfStartedOrEndWithText)
                End Sub

            Me.insertSerialNumberPane = New ElementControlPane(Of InsertSerialNumber)(c)
            Me.insertSerialNumberPane.Pane = ThisAddIn.Current.CustomTaskPanes.Add(Me.insertSerialNumberPane.Control, "Insert Serial Number", ThisAddIn.Current.Application.ActiveWindow)
            Me.insertSerialNumberPane.Pane.Width = 350
        End If

        Me.insertSerialNumberPane?.Show()
    End Sub

    Private Sub IncrementButton_Click(sender As Object, e As RibbonControlEventArgs) Handles IncrementButton.Click

        Dim app = ThisAddIn.Current.Application
        Dim target As Microsoft.Office.Interop.Excel.Range = app.Selection

        Try
            Dim upperCell As Microsoft.Office.Interop.Excel.Range = target.Offset(-1, 0)
            Dim nextCell As Microsoft.Office.Interop.Excel.Range = target.Offset(1, 0)
            Dim text As String = upperCell.Text

            Dim newText = Core.EditorString.IncrementText(text)
            If newText IsNot Nothing Then
                target.Formula = newText
                nextCell.Select()
            End If

        Catch ex2 As Exception
            MsgBox(My.Resources.Message1CannotIncrement, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation)
        End Try

    End Sub

    Private Sub InsertNowButton_Click(sender As Object, e As RibbonControlEventArgs) Handles InsertNowButton.Click

        Dim macaron As New ExcelMacaron(ThisAddIn.Current.Application)
        macaron.ReplaceSelectionText(
            Nothing,
            Sub(a)
                a.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.FFF")
            End Sub)

    End Sub

    Private Sub CopyNoLineBreakTextButton_Click(sender As Object, e As RibbonControlEventArgs) Handles CopyNoLineBreakTextButton.Click
        Dim result = New StringBuilder
        Dim app = ThisAddIn.Current.Application

        ' セルの形（左右　表としての位置、タブ区切りと改行で表現）を維持したままのコピー

        Dim getNewText =
            Function(text As String) As String
                Dim newText = text?.Replace(vbLf, "")
                Return newText
            End Function

        Dim macaron As New ExcelMacaron(app)
        macaron.ReplaceSelectionText(
            Nothing,
            Sub(a)
                If a.ColumnIndex > 1 Then
                    result.Append(vbTab)
                Else
                    If a.IsBox AndAlso a.RowIndex > 1 Then result.AppendLine("")
                End If
                result.Append(getNewText(a.Text))
            End Sub)

        System.Windows.Forms.Clipboard.SetText(result.ToString())
    End Sub

    Private Sub IncrementActiveButton_Click(sender As Object, e As RibbonControlEventArgs) Handles IncrementActiveButton.Click
        Dim app = ThisAddIn.Current.Application
        Dim target As Microsoft.Office.Interop.Excel.Range = app.Selection

        Dim text As String = target.Text
        Try
            Dim newText = Core.EditorString.IncrementText(text)
            If newText IsNot Nothing Then
                target.Formula = newText
            End If

        Catch ex As Exception
            MsgBox(My.Resources.Message1CannotIncrement, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation)
        End Try
    End Sub

    Private Sub IncrementMaxButton_Click(sender As Object, e As RibbonControlEventArgs) Handles IncrementMaxButton.Click

        Dim cell As Range = ThisAddIn.Current.Application.ActiveCell

        Try
            Dim table As ListObject = cell.ListObject
            If table IsNot Nothing Then
                Dim values =
                From column As ListColumn In table.ListColumns
                Where column.Range.Column = cell.Column
                From row As ListRow In table.ListRows
                Let range As Range = column.DataBodyRange()(row.Index)
                Let text As String = range.Text
                Where String.IsNullOrWhiteSpace(text) = False
                Let a = (From b In text.Matches(Of Core.WithNumberText)(Core.EditorString.Pattern) Select b).FirstOrDefault()
                Where a IsNot Nothing
                Order By a.before Descending
                Order By a.number Descending
                Select New With {.Text = text, .P = a}

                Dim max = values.FirstOrDefault()
                If max IsNot Nothing Then
                    max.P.number += 1
                    Dim newText = Core.EditorString.IncrementText(max.Text, max.P.number)

                    cell.Formula = newText
                Else
                    cell.Formula = "1"
                End If
            End If

        Catch ex As Exception
            MsgBox(My.Resources.Message1CannotIncrement, MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation)
        End Try

    End Sub

    Private Sub TrimEndButton_Click(sender As Object, e As RibbonControlEventArgs) Handles TrimEndButton.Click
        Dim app = ThisAddIn.Current.Application
        Dim myMacaron As New ExcelMacaron(app)
        myMacaron.ReplaceSelectionParagraphs(
            Nothing,
            Sub(a)
                a.Text = a.Text.TrimEnd()
            End Sub)
    End Sub


End Class
