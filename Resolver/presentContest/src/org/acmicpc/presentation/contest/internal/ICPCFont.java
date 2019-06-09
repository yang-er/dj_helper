package org.acmicpc.presentation.contest.internal;

import java.awt.Font;
import org.acmicpc.contest.Trace;

public class ICPCFont
{
    private static Font MASTER_FONT;

    public static Font getMasterFont()
    {
        if (MASTER_FONT != null) {
            return MASTER_FONT;
        }

        MASTER_FONT = new Font("DengXian", 1, 12);
        if (MASTER_FONT != null) {
            return MASTER_FONT;
        }

        MASTER_FONT = new Font("Microsoft YaHei", 1, 12);
        if (MASTER_FONT != null) {
            return MASTER_FONT;
        }

        MASTER_FONT = new Font("Source Han Sans", 1, 12);
        if (MASTER_FONT != null) {
            return MASTER_FONT;
        }

        Trace.trace((byte)3, "Error loading font");
        return MASTER_FONT;
    }
}
