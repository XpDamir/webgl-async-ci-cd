using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessCore
{
    public struct MoveRecord
    {
        public byte FromX;
        public byte FromY;
        public byte ToX;
        public byte ToY;

        public byte Promotion;
        public byte Flags;     

        public MoveRecord(Move move)
        {
            FromX = (byte)move.From.X;
            FromY = (byte)move.From.Y;
            ToX = (byte)move.To.X;
            ToY = (byte)move.To.Y;

            Promotion = move.Promotion == PieceType.None ? (byte)0 : (byte)move.Promotion;
            Flags = 0;
        }
    }
}
