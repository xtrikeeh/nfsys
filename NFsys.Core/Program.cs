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

            Console.WriteLine("Insira a série da NF");
            Console.Write("> ");
            string serie = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(serie))
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
                Console.WriteLine("> Erro: competência inválida. Certifique de utilizar o formato AAAA-MM.");
                return;
            }

            string[] notas_fiscais = Directory.GetFiles(@"C:\XmlNfce", "*.xml")
                .Where(arquivo =>
                {
                    string nome_arquivo = Path.GetFileName(arquivo);
                    string chave_acesso = nome_arquivo.Substring(0, 44);

                    return chave_acesso.Substring(20, 2) == "65" && chave_acesso.Substring(22, 3) == serie;
                })
                .ToArray();

            XNamespace namespace_sefaz = "http://www.portalfiscal.inf.br/nfe";

            List<NotaFiscal> nf_encontradas = new List<NotaFiscal>();

            string cnpj_emitente = "";

            foreach (var arquivo_nota_fiscal in notas_fiscais)
            {
                try
                {
                    XDocument xml_nf = XDocument.Load(arquivo_nota_fiscal);

                    if (xml_nf.Descendants(namespace_sefaz + "cStat").FirstOrDefault().Value != "100")
                    {
                        continue;
                    }

                    var tag_ide = xml_nf.Descendants(namespace_sefaz + "ide").FirstOrDefault();

                    var tag_serie = tag_ide.Element(namespace_sefaz + "serie").Value;

                    var tag_mod = tag_ide.Element(namespace_sefaz + "mod").Value;

                    if (tag_mod != "65")
                    {
                        continue;
                    }

                    var tag_dhEmi = tag_ide.Element(namespace_sefaz + "dhEmi").Value.Substring(0, 7);

                    if (serie.Substring(2, 1) == tag_serie && competencia == tag_dhEmi)
                    {
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

                        var tag_emit = xml_nf.Descendants(namespace_sefaz + "emit").FirstOrDefault();

                        cnpj_emitente = tag_emit.Element(namespace_sefaz + "CNPJ").Value;

                        var tag_infNFe = xml_nf.Descendants(namespace_sefaz + "infNFe").FirstOrDefault();

                        var chave_nf = tag_infNFe.Attribute("Id").Value.Replace("NFe", "");

                        var tag_vNF = decimal.Parse(xml_nf.Descendants(namespace_sefaz + "vNF")
                            .FirstOrDefault().Value, CultureInfo.InvariantCulture);

                        var dados_extraidos = new NotaFiscal
                        {
                            chave = chave_nf,
                            competencia = tag_dhEmi,
                            valor = tag_vNF
                        };

                        nf_encontradas.Add(dados_extraidos);
                    }
                }
                catch (XmlException)
                {
                    Console.WriteLine($"Erro ao ler o XML: '{arquivo_nota_fiscal}'");
                }
            }

            Console.WriteLine("\nProcessamento finalizado!");

            Console.WriteLine($"> {nf_encontradas.Count} NFs encontradas.");

            if (nf_encontradas.Count != 0)
            {
                var opcoes_json = new JsonSerializerOptions { WriteIndented = true };
                string arquivo_json = JsonSerializer.Serialize(nf_encontradas, opcoes_json);

                string mes = competencia.Substring(5, 2);
                string ano = competencia.Substring(0, 4);
                string cnpj = cnpj_emitente.Substring(0, 5);

                string nome_arquivo = $"{mes}{ano}{cnpj}.txt";

                File.WriteAllText(nome_arquivo, arquivo_json);

                Console.WriteLine($"> Arquivo '{nome_arquivo}' criado com sucesso!");
            }
        }
    }
}