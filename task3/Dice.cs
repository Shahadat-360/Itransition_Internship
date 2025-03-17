using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class Dice
    {
        public List<int> faces { get; set; }
        public Dice(List<int> faces)
        {
            if (faces == null || faces.Count == 0)
                throw new ArgumentException("Dice must have at least one face");
            this.faces = faces;
        }
        public int get_face_count()
        {
            return faces.Count;
        }
        public int get_face(int face_index)
        {
            return faces[face_index % faces.Count];
        }
        public override string ToString()
        {
            return string.Join(",", faces);
        }
    }
}
