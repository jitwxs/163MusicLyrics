using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicLyricApp.Bean
{
    public class ScalingFormConfig
    {
        /// <summary>
        /// 定义当前窗体的宽度
        /// </summary>
        public float X;
        
        /// <summary>
        /// 定义当前窗体的高度
        /// </summary>
        public float Y;

        public ScalingFormConfig(Control cons)
        {
            X = cons.Width;
            Y = cons.Height;
            
            SetTag(cons);
        }
        
        public void SetControls(Control cons)
        {
            var newX = cons.Width / X; //获取当前宽度与初始宽度的比例
            var newY = cons.Height / Y; //获取当前高度与初始高度的比例
            SetControls(newX, newY, cons);
        }

        private static void SetControls(float newx, float newy, Control cons)
        {
            //遍历窗体中的控件，重新设置控件的值
            foreach(Control con in cons.Controls)
            {
                //获取控件的Tag属性值，并分割后存储字符串数组
                if(con.Tag != null)
                {
                    var mytag = con.Tag.ToString().Split(';');
                    
                    //根据窗体缩放的比例确定控件的值
                    con.Width = Convert.ToInt32(Convert.ToSingle(mytag[0]) * newx); //宽度
                    con.Height = Convert.ToInt32(Convert.ToSingle(mytag[1]) * newy); //高度
                    con.Left = Convert.ToInt32(Convert.ToSingle(mytag[2]) * newx); //左边距
                    con.Top = Convert.ToInt32(Convert.ToSingle(mytag[3]) * newy); //顶边距
                    
                    var currentSize = Convert.ToSingle(mytag[4]) * newy; //字体大小
                    con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                    
                    if(con.Controls.Count > 0)
                    {
                        SetControls(newx, newy, con);
                    }
                }
            }
        }
        
        /// <summary>
        /// 控件大小随窗体大小等比例缩放,
        /// 在窗体重载中使用
        /// </summary>
        /// <param name="cons"></param>
        private static void SetTag(Control cons)
        {
            foreach(Control con in cons.Controls)
            {
                con.Tag = con.Width + ";" + con.Height + ";" + con.Left + ";" + con.Top + ";" + con.Font.Size;
                if(con.Controls.Count > 0)
                {
                    SetTag(con);
                }
            }
        }
    }
}