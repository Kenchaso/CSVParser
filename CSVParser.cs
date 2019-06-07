using System;
using System.Collections;

public class CSVParser
{
    /// <summary>
    /// CSV text state
    /// </summary>
    private enum CSVParserState
    {
        BEGIN_FIELD,
        PLAIN,
        QUOTE,
        END_FIELD,
    };

    /// <summary>
    /// Convert String to CSV
    /// </summary>
    /// <param name="csvText">string of CSV</param>
    /// <param name="removeFirstRow">Flag whether ignore the first row</param>
    /// <returns></returns>
    public static ArrayList ConvertCSV(string csvText, bool removeFirstRow = true)
    {
        int len = csvText.Length;
        int begin = 0;

        // If the text contains BOM, skip the first index.
        // Otherwise BOM will be included in the data
        while (Convert.ToInt32(csvText[begin]) == 0xFEFF && begin < len)
        {
            begin += 1;
        }

        ArrayList rows = new ArrayList();
        ArrayList cols = new ArrayList();

        int startIdx = begin;
        int lastIdx = begin;
        bool skipRow = removeFirstRow;
        bool needsQuoteEscape = false;

        CSVParserState state = CSVParserState.BEGIN_FIELD;

        for (int i = begin; i < len; ++i)
        {
            char c = csvText[i];
            switch (c)
            {
                case ',':
                    switch (state)
                    {
                        case CSVParserState.BEGIN_FIELD:
                            cols.Add("");
                            break;
                        case CSVParserState.PLAIN:
                        case CSVParserState.END_FIELD:
                            string item = csvText.Substring(startIdx, lastIdx + 1 - startIdx);
                            cols.Add(needsQuoteEscape ? item.Replace("\"\"", "\"") : item);
                            needsQuoteEscape = false;
                            state = CSVParserState.BEGIN_FIELD;
                            break;
                        case CSVParserState.QUOTE:
                            lastIdx = i;
                            break;
                    }
                    break;
                case ' ':
                case '\t':
                    switch (state)
                    {
                        case CSVParserState.PLAIN:
                            break;
                        case CSVParserState.QUOTE:
                            lastIdx = i;
                            break;
                        case CSVParserState.BEGIN_FIELD:
                        case CSVParserState.END_FIELD:
                            break;
                    }
                    break;
                case '\r':
                    if (i < len - 1 && csvText[i + 1] == '\n')
                    {
                        i += 1;
                    }
                    goto case '\n';
                // Fallthrough
                case '\n':
                    switch (state)
                    {
                        case CSVParserState.PLAIN:
                        case CSVParserState.END_FIELD:
                            string item = csvText.Substring(startIdx, lastIdx + 1 - startIdx);
                            cols.Add(needsQuoteEscape ? item.Replace("\"\"", "\"") : item);
                            needsQuoteEscape = false;
                            if (!skipRow)
                            {
                                cols.TrimToSize();
                                rows.Add(cols);
                            }
                            else
                            {
                                skipRow = false;
                            }
                            cols = new ArrayList(cols.Count);
                            state = CSVParserState.BEGIN_FIELD;
                            break;
                        case CSVParserState.BEGIN_FIELD:
                            if (cols.Count > 0)
                            {
                                cols.Add("");
                                if (!skipRow)
                                {
                                    cols.TrimToSize();
                                    rows.Add(cols);
                                }
                                else
                                {
                                    skipRow = false;
                                }
                                cols = new ArrayList(cols.Count);
                            }
                            break;
                        case CSVParserState.QUOTE:
                            lastIdx = i;
                            break;
                    }
                    break;
                case '"':
                    switch (state)
                    {
                        case CSVParserState.BEGIN_FIELD:
                            startIdx = i + 1;
                            lastIdx = i;
                            state = CSVParserState.QUOTE;
                            break;
                        case CSVParserState.END_FIELD:
                        case CSVParserState.PLAIN:
                            throw new ApplicationException("Incorrect format CSV.");
                        case CSVParserState.QUOTE:
                            if (i < len - 1)
                            {
                                if (csvText[i + 1] == '"')
                                {
                                    i += 1;
                                    needsQuoteEscape = true;
                                    lastIdx = i;
                                }
                                else
                                {
                                    state = CSVParserState.END_FIELD;
                                }
                            }
                            else
                            {
                                state = CSVParserState.END_FIELD;
                            }
                            break;
                    }
                    break;
                default:
                    switch (state)
                    {
                        case CSVParserState.BEGIN_FIELD:
                            startIdx = i;
                            lastIdx = i;
                            state = CSVParserState.PLAIN;
                            break;
                        case CSVParserState.END_FIELD:
                            throw new System.ApplicationException("Could not parse CSV: extra character found outside quotation.");
                        case CSVParserState.PLAIN:
                        case CSVParserState.QUOTE:
                            lastIdx = i;
                            break;
                    }
                    break;
            }
        }

        switch (state)
        {
            case CSVParserState.BEGIN_FIELD:
                if (cols.Count > 0 && !skipRow)
                {
                    cols.Add("");
                    cols.TrimToSize();
                    rows.Add(cols);
                }
                break;
            case CSVParserState.END_FIELD:
            case CSVParserState.PLAIN:
                if (!skipRow)
                {
                    cols.Add(csvText.Substring(startIdx, lastIdx + 1 - startIdx));
                    cols.TrimToSize();
                    rows.Add(cols);
                }
                break;
            case CSVParserState.QUOTE:
                throw new System.ApplicationException("Incorrect format CSV.");
        }
        return rows;
    }
}