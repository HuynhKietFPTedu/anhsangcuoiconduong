using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        // Chuỗi HTML tổng hợp chứa nhiều phần tử
        var namefile = "quan6.txt";
        var html = File.ReadAllText($"D:\\FPT Polytechnic\\PDP201\\abc\\abc\\{namefile}", Encoding.UTF8);
        // Tạo HtmlDocument từ chuỗi HTML tổng hợp
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Tìm và tách các phần tử riêng lẻ
        var listings = doc.DocumentNode.SelectNodes("//div[contains(@class, 'listing-summary')]");

        // Đường dẫn tới file JSON
        string filePath = "D:\\FPT Polytechnic\\PDP201\\Website\\CrawlData\\Data\\data.json";

        List<dynamic> dataList;

        // Kiểm tra nếu file JSON đã tồn tại
        if (File.Exists(filePath))
        {
            // Đọc nội dung file JSON
            var jsonData = File.ReadAllText(filePath, Encoding.UTF8);
            dataList = JsonConvert.DeserializeObject<List<dynamic>>(jsonData);
        }
        else
        {
            // Nếu file chưa tồn tại, tạo danh sách mới
            dataList = new List<dynamic>();
        }

        // Khởi tạo id tự tăng
        int idCounter = dataList.Count + 1;

        // Lặp qua các phần tử riêng lẻ và xử lý
        foreach (var listing in listings)
        {
            // Tạo một HtmlDocument mới từ phần tử riêng lẻ
            HtmlDocument listingDoc = new HtmlDocument();
            listingDoc.LoadHtml(listing.OuterHtml);

            // Extract thông tin

            var imgNode = listingDoc.DocumentNode.SelectSingleNode("//a/img");
            string imgSrc = imgNode?.GetAttributeValue("src", "https://www.thegioituthien.com/media/com_mtree/images/noimage_thb.png").Trim();

            var nameNode = listingDoc.DocumentNode.SelectSingleNode("//div[@class='header']/h3/a/span");
            string name = nameNode?.InnerText.Trim();

            var phoneNode = listingDoc.DocumentNode.SelectSingleNode("//div[@id='field_30']/span[@class='output']");
            string phone = phoneNode?.InnerText.Trim();

            var addressNode = listingDoc.DocumentNode.SelectSingleNode("//p[@class='address']");
            string address = addressNode?.InnerText.Trim();

            var websiteNode = listingDoc.DocumentNode.SelectSingleNode("//p[@class='website']/a");
            string website = websiteNode?.InnerText.Trim();

            var descriptionNodes = listingDoc.DocumentNode.SelectNodes("//p");
            string description = descriptionNodes?.OrderByDescending(p => p.InnerText.Length).FirstOrDefault()?.InnerText.Trim().Replace("\"", "").Replace("\n", "").Replace("\r", "");

            // Tách tỉnh, quận từ address
            var location = ExtractLocation(addressNode);

            // Tạo JSON object mới
            var newData = new
            {
                Id = idCounter,
                Name = name,
                Phone = phone,
                Website = website,
                Img = imgSrc,
                Description = description,
                Address = address,
                Province = location.Province,
                District = location.District
            };

            // Thêm phần tử mới vào danh sách
            dataList.Add(newData);

            // Cập nhật id tự tăng
            idCounter++;
        }

        // Ghi lại danh sách vào file JSON
        var jsonSettings = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.Default // Giữ nguyên ký tự Unicode
        };

        var updatedJsonData = JsonConvert.SerializeObject(dataList, Formatting.Indented, jsonSettings);
        File.WriteAllText(filePath, updatedJsonData, Encoding.UTF8);

        Console.OutputEncoding = System.Text.Encoding.UTF8; // Đảm bảo console hiển thị Unicode
        Console.WriteLine(updatedJsonData);
    }

    static dynamic ExtractLocation(HtmlNode addressNode)
    {
        string address = addressNode?.InnerText.Trim();
        string province = null;
        string district = null;

        // Extract province
        if (address.Contains("TP HCM")) province = "TP HCM";
        else if (address.Contains("Hà Nội")) province = "Hà Nội";
        else if (address.Contains("TP Thủ Đức")) province = "TP Thủ Đức";

        // Extract district from anchor tag
        var districtNode = addressNode.SelectSingleNode("//a[contains(@href, '/browse-by/city.html')]");
        if (districtNode != null)
        {
            district = districtNode.InnerText.Trim();
        }
        return new { Province = province, District = district };
    }
}
