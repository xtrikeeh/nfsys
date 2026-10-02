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
            string serie = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(serie) )
            {
                Console.WriteLine("> Erro: série inválida.");
                return;
            }

            Console.WriteLine("Agora, insira a competência (AAAA-MM)");
            Console.Write("> ");
            string competencia = Console.ReadLine()?.Trim() ?? "";

            if (!DateTime.TryParseExact(
                competencia,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            {
                Console.WriteLine("> Erro: competência inválida. Utilize o formato AAAA-MM.");
                return;
            }

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

                    if (tag_ide == null)
                    {
                        Console.WriteLine($"> Erro: XML sem a tag 'ide': '{arquivo_nota_fiscal}'");
                        continue;
                    }

                    var tag_serie = tag_ide.Element(namespace_sefaz + "serie");
                    var tag_dhEmi = tag_ide.Element(namespace_sefaz + "dhEmi");

                    if (tag_serie == null || tag_dhEmi == null)
                    {
                        Console.WriteLine($"> Erro: XML com informações incompletas: '{arquivo_nota_fiscal}'");
                        continue;
                    }

                    string serie_xml = tag_serie.Value;
                    string competencia_xml = tag_dhEmi.Value.Substring(0, 7);

                    if (serie == serie_xml && competencia == competencia_xml)
                    {
                        var tag_infNFe = xml_nf.Descendants(namespace_sefaz + "infNFe").FirstOrDefault();

                        if (tag_infNFe == null || tag_infNFe.Attribute("Id") == null)
                        {
                            Console.WriteLine($"> Erro: XML sem chave de acesso: '{arquivo_nota_fiscal}'");
                            continue;
                        }

                        var chave_nf = tag_infNFe.Attribute("Id").Value.Replace("NFe", "");

                        var competencia_nf = competencia_xml;

                        var tag_vNF = xml_nf.Descendants(namespace_sefaz + "vNF").FirstOrDefault();

                        if (tag_vNF == null)
                        {
                            Console.WriteLine($"> Erro: XML sem a tag 'vNF': '{arquivo_nota_fiscal}'");
                            continue;
                        }

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

            if (nf_encontradas.Count != 0)
            {
                var opcoes_json = new JsonSerializerOptions { WriteIndented = true };
                string arquivo_json = JsonSerializer.Serialize(nf_encontradas, opcoes_json);

                File.WriteAllText("envio.json", arquivo_json);

                Console.WriteLine("> Arquivo 'envio.json' criado com sucesso!");
            }
        }
    }
}
