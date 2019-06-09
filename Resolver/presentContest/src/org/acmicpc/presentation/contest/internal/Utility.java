package org.acmicpc.presentation.contest.internal;

import java.awt.Color;
import java.awt.Graphics2D;

public class Utility
{
    public static Color[] getColorsBetween(Color c1, Color c2, int steps)
    {
        Color[] colors = new Color[steps];
        float[] c1rgb = c1.getRGBComponents(null);
        float[] c2rgb = c2.getRGBComponents(null);

        float[] f = new float[4];
        for (int i = 0; i < steps; i++)
        {
            float x = i / (steps - 1);
            for (int j = 0; j < 4; j++) {
                f[j] = (c1rgb[j] * (1.0F - x) + c2rgb[j] * x);
            }
            colors[i] = new Color(f[0], f[1], f[2], f[3]);
        }
        return colors;
    }

    public static Color alpha(Color c, int alpha)
    {
        return new Color(c.getRed(), c.getGreen(), c.getBlue(), alpha);
    }

    public static Color darker(Color c, float f)
    {
        return new Color(Math.max((int)(c.getRed() * f), 0), Math.max((int)(c.getGreen() * f), 0), Math.max(
                (int)(c.getBlue() * f), 0));
    }

    public static Color alphaDarker(Color c, int alpha, float f)
    {
        return new Color(Math.max((int)(c.getRed() * f), 0), Math.max((int)(c.getGreen() * f), 0), Math.max(
                (int)(c.getBlue() * f), 0), alpha);
    }

    public static void drawString3D(Graphics2D g, String s, float x, float y)
    {
        Color c = g.getColor();
        g.setColor(alpha(Color.BLACK, c.getAlpha() / 3));
        g.drawString(s, x - 1.0F, y - 1.0F);

        g.drawString(s, x + 1.0F, y + 1.0F);
        g.setColor(c);
        g.drawString(s, x, y);
    }

    public static void drawString3D3(Graphics2D g, String s, float x, float y)
    {
        Color c = g.getColor();
        g.setColor(Color.BLACK);
        g.drawString(s, x - 2.0F, y - 2.0F);
        g.drawString(s, x - 2.0F, y + 2.0F);
        g.drawString(s, x + 2.0F, y - 2.0F);
        g.drawString(s, x + 2.0F, y + 2.0F);
        g.setColor(c);
        g.drawString(s, x, y);
    }

    public static void drawString3DWhite(Graphics2D g, String s, float x, float y)
    {
        Color c = g.getColor();
        g.setColor(alpha(Color.WHITE, c.getAlpha() / 2));
        g.drawString(s, x - 1.0F, y - 1.0F);

        g.drawString(s, x + 1.0F, y + 1.0F);
        g.setColor(c);
        g.drawString(s, x, y);
    }
}
