using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoLevelPlus
{

    class LoLBuilder
    {
        public static int[] GetSkillSequence(string cname)
        {
            WebClient pbClient = new WebClient();
            String Data = null;
            try
            {
                Data = pbClient.DownloadString("http://lolbuilder.net/" + cname);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            String SkillSeq = ExtractString(Data, "window.skillOrder[0] = [", "];");
            string[] seqinstringarray = SkillSeq.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int[] OrderedSequence = Array.ConvertAll(seqinstringarray, s => int.Parse(s));
            
            for (int i = 0; i < seqinstringarray.Length; i++)
            {
                try
                {
                    OrderedSequence[i] = int.Parse(seqinstringarray[i]);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }

                return OrderedSequence;
            }

            return null;
        }

        private static string ExtractString(string s, string start, string end)
        {
            if (s.Contains(start) && s.Contains(end) && start.Length > 0)
            {
                int startIndex = s.IndexOf(start) + start.Length;
                int endIndex = s.IndexOf(end, startIndex);

                return s.Substring(startIndex, endIndex - startIndex);
            }

            return "";
        }
    }
}
