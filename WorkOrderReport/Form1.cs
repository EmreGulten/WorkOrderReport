using System.Configuration;
using System.Data;
using WorkOrderReport.BL;
using WorkOrderReport.Models;

namespace WorkOrderReport
{
    public partial class Form1 : Form
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MainConnection"].ToString();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            

        }

        private void btnRapor_Click(object sender, EventArgs e)
        {
            // veritabanýndan iþ emirleri ve duruþ verileri alýnýr
            List<WorkOrderModel> workOrders = WorkOrderBL.GetWorkOrders(connectionString);
            List<StopoverModel> stopovers = StopoversBL.GetStopovers(connectionString);

            var reportItems = new Dictionary<int, Dictionary<string, int>>(); // raporlama iþlemi için reportItems adlý bir sözlük oluþturuyorum. Bu nesne her bir iþ emri için duruþ nedenleri ve bu nedenlere ait toplam sürelerin saklandýðý bir sözlük olarak kullanýlacak.

            var reasons = new List<string>(); // duruþlarýn nedenlerini tutmak için bir liste 

            foreach (var stopover in stopovers)
            {
                if (!reasons.Contains(stopover.Reason)) // listesinde yoksa, reasons listesine eklenir.
                {
                    reasons.Add(stopover.Reason); // Duruþ nedenleri bir listeye ekliyorum.
                    /* Bu döngü stopovers listesindeki her bir duruþ verisi için döner ve her bir duruþ verisinin nedenini reasons listesine ekler. 
                     * Bu ileride raporlama iþlemi için kullanacaðým olan duruþ nedenlerinin bir listesini oluþturur. */
                }

                //Eþleþen iþ emirleri ve duruþ nedenleri için raporlama verileri oluþturulur Bu kod bloðu her bir duruþ verisi için eþleþen iþ emirlerini bulur ve her bir iþ emri için duruþ nedenlerine göre raporlama verileri oluþturur. Öncelikle, her bir iþ emri ve duruþ verisi arasýndaki zaman çak

                var matchedOrders = workOrders.Where(o => stopover.Start < o.End && o.Start < stopover.End).ToArray(); // duruþun tarih aralýðýna uyan iþ emirlerini bulup ve matchedOrders adlý bir diziye atýyorum

                foreach (var item in matchedOrders) // örtüþen her iþ emri için bir döngü baþlatýlýr
                {
                    var defaultEndDate = stopover.End;
                    var defaultStartDate = stopover.Start;
                    /* birden fazla kullanacaðým için duruþun baþlangýç ve bitiþ tarihlerini deðiþkene alýyorum karýþýklýk olmasýn diye */

                    if (item.Start > stopover.Start) //  iþ emrinin baþlangýç tarihi duruþun baþlangýç tarihinden büyüks, defaultStartDate deðiþkenine iþ emrinin baþlangýç tarihi atanýr.
                        defaultStartDate = item.Start;

                    if (item.End < stopover.End) // iþ emrinin bitiþ tarihi duruþun bitiþ tarihinden küçükse defaultEndDate deðiþkenine iþ emrinin bitiþ tarihi atanýr.
                        defaultEndDate = item.End;

                    var totalStopoverMinutes = (int)defaultEndDate.Subtract(defaultStartDate).TotalMinutes; // dakika olarak duruþun örtüþtüðü süre hesaplanýr ve totalStopoverMinutes deðiþkenine atanýr.

                    if (reportItems.ContainsKey(item.Id)) // Bu satýr, iþ emri olup olmadýðýný kontrol eder. Bu kontrol, daha önce eklendi mi yoksa ekleme yapýlmasý mý gerektiðini belirlemek için yapýlýr.
                    {
                        var items = reportItems[item.Id];
                        var existsData = items.FirstOrDefault(i => i.Key == stopover.Reason); //  bir üstte  duruþ nedenleri ile eþleþen bir anahtarýn var olup olmadýðýný kontrol eder. Eðer varsa, bu anahtar ve deðerini existsData deðiþkeninde saklar.
                        if (!existsData.Equals(default(KeyValuePair<string, int>)))  // deðiþkeninin boþ olup olmadýðýný kontrol eder.
                        {
                            items.Remove(existsData.Key); // Eðer bu deðer zaten varsa, bu blok içindeki kod, bu anahtarý kullanarak sözlükteki deðeri günceller.
                            items.Add(stopover.Reason, existsData.Value + totalStopoverMinutes);
                            reportItems[item.Id] = items;
                            continue;
                        }

                        reportItems[item.Id].Add(stopover.Reason, totalStopoverMinutes);
                        continue;
                    }

                    reportItems.Add(item.Id, new Dictionary<string, int>() {
                        { stopover.Reason, totalStopoverMinutes }
                    });

                }
            }



            var dt = new DataTable();
            var columns = reasons.ToList(); // duruþ nedenlerini alýyorum kolonlara daðýtmak için.

            //Birden fazla kullanýldýðý için deðiþkene aldým.
            var orderFieldName = "Ýþ Emri";
            var totalFieldName = "Toplam";

            columns.Insert(0, orderFieldName);
            columns.Add(totalFieldName);

            foreach (var col in columns) // duruþ nedenlerini kolonlara alýyorum.
                dt.Columns.Add(col);

            //freeOrders adlý bir liste oluþturarak herhangi bir duruþa girmemiþ iþ emirlerini belirliyorum
            var freeOrders = workOrders.Select(o => o.Id).ToList().Except(reportItems.Select(rp => rp.Key).ToList()).ToList();

            //herhangi bir duruþa girmemiþ iþ emirlerini  reportItems sözlüðüne eklenmesi gerekiyor. Bunu aþaðýda yapýyorum.
            foreach (var fo in freeOrders)
                reportItems.Add(fo, new Dictionary<string, int>() { });

            foreach (var item in reportItems)
            {
                var row = dt.NewRow(); // her bir öðe için dt tablosuna bir satýr ekliyorum.
                foreach (var col in columns)
                    row[col] = "...";  // satýrýn her bir hücresine varsayýlan olarak "..." deðeri atanýr.

                row[0] = item.Key; // iþ emri sütununa mevcut iþ emri id sini ekliyorum.
                var total = 0;
                foreach (var reason in item.Value)
                {
                    row[reason.Key] = reason.Value;
                    total += reason.Value;
                }

                row[totalFieldName] = total;
                dt.Rows.Add(row);
            }


            // toplam deðerlerin yazýlacaðý son satýr 
            var totalRow = dt.NewRow();
            totalRow[orderFieldName] = totalFieldName;
            foreach (var reason in reasons)
                totalRow[reason] = dt.AsEnumerable().Sum(r => r[reason].ToString() == "..." ? 0 : int.Parse(r[reason].ToString()));

            //Eðer ... var ise sýfýr olarak alalým
            totalRow[totalFieldName] = dt.AsEnumerable().Sum(r => r[totalFieldName].ToString() == "..." ? 0 : int.Parse(r[totalFieldName].ToString()));
            dt.Rows.Add(totalRow);

            dtGrid.DataSource = dt;

            //Karýþýk eklediðimiz iþ emirlerini sýralayalým
            dtGrid.Sort(dtGrid.Columns[orderFieldName], System.ComponentModel.ListSortDirection.Ascending);
        }
    }
}