﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UltimaCore.Graphics
{
    public sealed class Map : IDisposable
    {
        public static Map Felucca { get; } = new Map(0, 7168, 4096);



        private TileMatrix _tiles;

        public Map(int map, int width, int height)
        {
            Index = map; Width = width; Height = height;
        }

        public int Index { get; }
        public int Width { get; }
        public int Height { get; }
        public TileMatrix Tiles => _tiles;


        public void Load()
        {
            if (_tiles == null)
                _tiles = new TileMatrix(Index, Width, Height);
        }

        public short[] Render(int x, int y, int width, int height)
        {
            x = x >> 3;
            y = y >> 3;
            //width = width << 3;
            //height = height << 3;

            short[] result = new short[width * height * 64];
            unsafe
            {
                fixed (short* presult = result)
                {
                    short* pvresut = presult;

                    for (int oy = 0, by = y; oy < height; oy++, by++)
                    {
                        for (int ox = 0, bx = x; ox < width; ox++, bx++)
                        {
                            short[] data = RenderBlock(bx, by, true);

                            fixed (short* pdata = data)
                            {
                                short* pvdata = pdata;

                                for (int i = 0; i < 64; i++)
                                    *pvresut++ = *pvdata++;
                            }
                        }
                    }
                }
            }
           
            return result;
        }

        private unsafe short[] RenderBlock(int x, int y, bool drawstatics)
        {
            short[] data = new short[64];

            Tile[] tiles = _tiles.GetLandBlock(x, y);

            fixed (Tile* ptTiles = tiles)
            {
                Tile* ptiles = ptTiles;

                fixed(short* pdata = data)
                {
                    short* pvdata = pdata;

                    if (drawstatics)
                    {
                        HuedTile[][][] statics = _tiles.GetStaticBlock(x, y);

                        for (int k = 0, v = 0; k < 8; k++, v += 8)
                        {
                            for (int p = 0; p < 8; p++)
                            {
                                int highTop = -255;
                                int highZ = -255;
                                int highID = 0;
                                int highHue = 0;
                                int z, top;
                                bool highstatic = false;

                                HuedTile[] curStatics = statics[p][k];

                                if (curStatics.Length > 0)
                                {
                                    fixed (HuedTile* phtStatics = curStatics)
                                    {
                                        HuedTile* pStatics = phtStatics;
                                        HuedTile* pStaticsEnd = pStatics + curStatics.Length;

                                        while (pStatics < pStaticsEnd)
                                        {
                                            z = pStatics->Z;                                           
                                            top = z + TileData.StaticData[pStatics->ID].Height;

                                            if (top > highTop || (z > highZ && top >= highTop))
                                            {
                                                highTop = top;
                                                highZ = z;
                                                highID = pStatics->ID;
                                                highHue = pStatics->Hue;
                                                highstatic = true;
                                            }

                                            ++pStatics;
                                        }
                                    }
                                }

                                StaticTile[] pending = _tiles.GetPendingStatics(x, y);
                                if (pending != null)
                                {
                                    foreach (StaticTile pens in pending)
                                    {
                                        if (pens.X == p && pens.Y == k)
                                        {
                                            z = pens.Z;
                                            top = z + TileData.StaticData[pens.ID].Height;

                                            if (top > highTop || (z > highZ && top >= highTop))
                                            {
                                                highTop = top;
                                                highZ = z;
                                                highID = pens.ID;
                                                highHue = pens.Hue;
                                                highstatic = true;
                                            }
                                        }
                                    }
                                }

                                top = ptiles->Z;

                                if (top > highTop)
                                {
                                    highID = ptiles->ID;
                                    highHue = 0;
                                    highstatic = false;
                                }

                                if (highHue == 0)
                                {
                                    if (highstatic)
                                        *pvdata++ = (short)Hues.GetRadarColorData(highID + 0x4000);
                                    else
                                        *pvdata++ = (short)Hues.GetRadarColorData(highID);
                                }
                                else                              
                                    *pvdata++ = (short)Hues.GetColor((ushort)(highHue - 1), (ushort)((Hues.GetRadarColorData(highID + 0x4000) >> 10) & 0x1F));
                                

                                ++ptiles;
                            }
                        }
                    }
                    else
                    {
                        Tile* pend = ptiles + 64;
                        while (ptiles < pend)
                            *pvdata++ = (short)Hues.GetRadarColorData((ptiles++)->ID);
                    }
                }
            }

            return data;
        }

        public void Dispose()
        {
            
        }
    }
}