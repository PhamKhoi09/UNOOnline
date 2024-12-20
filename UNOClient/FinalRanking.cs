using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnoOnline
{
    public partial class FinalRanking : Form
    {
        public FinalRanking()
        {
            InitializeComponent();
            DisplayRanking(GameManager.Instance.Players);
        }
        public void DisplayRanking(List<Player> players)
        {
            // Sắp xếp danh sách người chơi theo điểm số giảm dần
            players.Sort((x, y) => y.Points.CompareTo(x.Points));
            // Hiển thị danh sách người chơi top 1 2 3 4  và điểm của họ
            for (int i = 0; i < players.Count; i++)
            {
                if (i == 0)
                {
                    NameTop1.Text = players[i].Name;
                    Point1.Text = players[i].Points.ToString();
                }
                else if (i == 1)
                {
                    NameTop2.Text = players[i].Name;
                    Point2.Text = players[i].Points.ToString();
                }
                else if (i == 2)
                {
                    NameTop3.Text = players[i].Name;
                    Point3.Text = players[i].Points.ToString();
                }
                else if (i == 3)
                {
                    NameTop4.Text = players[i].Name;
                    Point4.Text = players[i].Points.ToString();
                }
            }
        }
        private void FinalRanking_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
