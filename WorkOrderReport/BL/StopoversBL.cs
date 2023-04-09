using Microsoft.Data.SqlClient;
using WorkOrderReport.Models;

namespace WorkOrderReport.BL
{
    public static class StopoversBL
    {
        public static List<StopoverModel> GetStopovers(string connection)
        {
            List<StopoverModel> stopoversList = new List<StopoverModel>();

            using (SqlConnection con = new SqlConnection(connection))
            {
                SqlCommand cmd = new();
                SqlDataReader dr;
                cmd.Connection = con;
                con.Open();
                cmd.CommandText = "select * from tblStopovers";
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    StopoverModel stopover = new();
                    stopover.Reason = Convert.ToString(dr["Reason"]);
                    stopover.Start = Convert.ToDateTime(dr["start"]);
                    stopover.End = Convert.ToDateTime(dr["end"]);
                    stopoversList.Add(stopover);
                }

                dr.Close();
                con.Close();
            }

            return stopoversList;
        }
    }
}
