using NFsys.Core.Models;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace NFsys.Core
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bem-vindo ao NFsys\n");

            Console.WriteLine("Insira o número de série da NF");
            Console.Write("> ");
            string serie = Console.ReadLine();

            Console.WriteLine("Agora, insira a competência (AAAA-MM)");
            Console.Write("> ");
            string ano_mes = Console.ReadLine().Trim();

            string[] notas_fiscais = Directory.GetFiles(@"C:\XmlNfce", "*.xml");

            XNamespace namespace_sefaz = "http://www.portalfiscal.inf.br/nfe";

            List<NotaFiscal> nf_encontradas = new List<NotaFiscal>();

            foreach (var arquivo_nota_fiscal in notas_fiscais)
            {
                try
                {
                    XDocument xml_nf = XDocument.Load(arquivo_nota_fiscal);

                    var tag_dest = xml_nf.Descendants(namespace_sefaz + "dest").FirstOrDefault();

                    if (tag_dest != null)
                    {
                        var tag_cpf = tag_dest.Element(namespace_sefaz + "CPF");
                        var tag_cnpj = tag_dest.Element(namespace_sefaz + "CNPJ");

                        if (tag_cpf != null || tag_cnpj != null)
                        {
                            continue;
                        }
                    }

                    var tag_ide = xml_nf.Descendants(namespace_sefaz + "ide").FirstOrDefault();

                    string tag_serie = tag_ide.Element(namespace_sefaz + "serie").Value;
                    string tag_dhEmi = tag_ide.Element(namespace_sefaz + "dhEmi").Value.Substring(0, 7);

                    if (serie == tag_serie && ano_mes == tag_dhEmi)
                    {
                        var tag_infNFe = xml_nf.Descendants(namespace_sefaz + "infNFe").FirstOrDefault();
                        var chave_nf = tag_infNFe.Attribute("Id").Value.Replace("NFe", "");

                        var competencia_nf = tag_dhEmi;

                        var tag_vNF = xml_nf.Descendants(namespace_sefaz + "vNF").FirstOrDefault();

                        decimal valor_nf = decimal.Parse(
                            tag_vNF.Value,
                            CultureInfo.InvariantCulture
                        );

                        var dados_extraidos = new NotaFiscal
                        {
                            chave = chave_nf,
                            competencia = competencia_nf,
                            valor = valor_nf
                        };

                        nf_encontradas.Add(dados_extraidos);
                    }
                }
                catch (XmlException)
                {
                    Console.WriteLine($"XML inválido: '{arquivo_nota_fiscal}'");
                }
            }

            Console.WriteLine("\nProcessamento finalizado!");

            Console.WriteLine($"> {nf_encontradas.Count} NFs encontradas.");

            var opcoes_json = new JsonSerializerOptions { WriteIndented = true };
            string arquivo_json = JsonSerializer.Serialize(nf_encontradas, opcoes_json);

            File.WriteAllText("envio.json", arquivo_json);

            Console.WriteLine("> Arquivo 'envio.json' criado com sucesso!");
        }
    }
}
