﻿using System.Collections;

namespace CryptoSbmScanner;

public class ListViewColumnSorter : IComparer
{
    public int SortColumn { set; get; } = 0;
    public SortOrder SortOrder { set; get; } = SortOrder.Descending;

    internal CaseInsensitiveComparer ObjectCompare = new();

    public virtual int Compare(object x, object y)
    {
        return 0;
    }


    public void ClickedOnColumn(int column)
    {
        // Determine if clicked column is already the column that is being sorted.
        if (column == SortColumn)
        {
            // Reverse the current sort direction
            if (SortOrder == SortOrder.Ascending)
                SortOrder = SortOrder.Descending;
            else
                SortOrder = SortOrder.Ascending;
        }
        else
        {
            // Set the column number that is to be sorted
            SortColumn = column;
            SortOrder = SortOrder.Ascending;
        }
    }
}
