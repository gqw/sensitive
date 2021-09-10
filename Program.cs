using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace sensitive
{
    class Program
    {
        static void Main(string[] args)
        {
            var ac = new AcSensitive();
            ac.AddPattern("her");
            ac.AddPattern("say");
            ac.AddPattern("she");
            ac.AddPattern("shr");
            ac.AddPattern("he");
            ac.AddPattern("sm");
            ac.AddPattern("大戰三國");
            ac.AddPattern("毛泽东", true);
            ac.Build();

            var acDraw = new AcDraw(1800, 1800);
            acDraw.show_root_faile_connection = false;
            acDraw.drawAcNodes(ac.ac_root_);
            acDraw.save();

            //var ret = ac.Check("he", true);
            //if (ret == false)
            //    throw new ApplicationException("error");

            //ret = ac.Check("we like he", true);
            //if (ret == false)
            //    throw new ApplicationException("error");

            //ret = ac.Check("haha, we are good", true);
            //if (ret == true)
            //    throw new ApplicationException("error");


            //var sret = ac.Filter("毛泽冬 ", "***", true);
            //if (sret != "*** ")
            //    throw new ApplicationException("error");
            
            var sret = ac.Filter("we 大戰三國 LIKE he heheDA HE WO sm small kism tick mao ze dong maozedong 毛泽东 毛泽冬 ", "***", true);
            if (sret != "we *** LIKE *** heheDA *** WO *** small kism tick *** *** *** *** ")
                throw new ApplicationException("error");
            return;
        }
    }
}
