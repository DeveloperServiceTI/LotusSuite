using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace LotoEngine.Core.Utils
{
    public static class LotoTools
    {


        /// <summary>
        /// 1 - MEGA SENA
        /// 2 - LOTOFACIL
        /// 3 - +MILIONARIA
        /// </summary>
        /// <param name="typeGame"></param>
        /// <param name="porOrdemCrescente"></param>
        /// <returns></returns>
        public static List<int> CarregarHistoricoJogos(int typeGame, bool porOrdemCrescente = false)
        {
            //https://asloterias.com.br/download-todos-resultados-mega-sena
            //https://asloterias.com.br/download-todos-resultados-lotofacil

            string ordemInfo = null;
            if (porOrdemCrescente)
            {
                ordemInfo = "crescente";
            }
            else
            {
                ordemInfo = "sorteio";
            }

            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string arquivoExcelHistorico = Path.Combine(exePath, "LOTERICA"
                , (typeGame == 1) ?
                $"mega_sena_{ordemInfo}.xlsx"
                : ((typeGame == 2) ?
                    $"loto_facil_{ordemInfo}.xlsx"
                    : ""
                    )
                );
            List<int> HistoricoJogos = CarregaJogosSorteados(arquivoExcelHistorico);
            return HistoricoJogos;
        }

        public static List<List<int>> CarregarHistoricoDaLista(List<string> HistoricoJogos)
        {
            var historico = new List<List<int>>();

            foreach (var linha in HistoricoJogos)
            {
                var numeros = linha.Split(',').Select(int.Parse).ToList();
                historico.Add(numeros);
            }
            //historico.Reverse(); //ESTA SENDO FEITO NO USO DO HISTORICO: inverte a posicao da lista / os primeiros da lista saos dos ultimos sorteio
            return historico;
        }


        #region [STRING]

        /// <summary>
        /// 1 - MEGA SENA
        /// 2 - LOTOFACIL
        /// 3 - +MILIONARIA
        /// </summary>
        /// <param name="typeGame"></param>
        /// <param name="porOrdemCrescente"></param>
        /// <returns></returns>
        public static List<string> CarregarHistoricoJogosString(int typeGame, bool porOrdemCrescente = false)
        {
            //https://asloterias.com.br/download-todos-resultados-mega-sena
            //https://asloterias.com.br/download-todos-resultados-lotofacil

            string ordemInfo = null;
            if (porOrdemCrescente)
            {
                ordemInfo = "crescente";
            }
            else
            {
                ordemInfo = "sorteio";
            }

            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string arquivoExcelHistorico = Path.Combine(exePath, "LOTERICA"
                , (typeGame == 1) ?
                $"mega_sena_{ordemInfo}.xlsx"
                : ((typeGame == 2) ?
                    $"loto_facil_{ordemInfo}.xlsx"
                    : ""
                    )
                );
            List<string> HistoricoJogos = CarregaJogosSorteadosString(arquivoExcelHistorico);
            return HistoricoJogos;
        }


        static List<string> CarregaJogosSorteadosString(string caminhoPlanilha)
        {
            if (!File.Exists(caminhoPlanilha))
            {
                return new List<string>();
            }
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                FileInfo arquivoExcel = new FileInfo(caminhoPlanilha);
                if (!arquivoExcel.Exists)
                {
                    Console.WriteLine("Arquivo Excel não encontrado.");
                    return new List<string>();
                }

                using (ExcelPackage pacote = new ExcelPackage(arquivoExcel))
                {
                    ExcelWorksheet planilha = pacote.Workbook.Worksheets.FirstOrDefault();
                    if (planilha == null)
                    {
                        Console.WriteLine("Planilha não encontrada no arquivo.");
                        return new List<string>();
                    }

                    HashSet<string> jogosSorteados = new HashSet<string>();
                    int colunas = 17;//planilha.Dimension.End.Column;
                    int linhas = planilha.Dimension.End.Row;

                    // Lê todos os jogos da planilha
                    //for (int i = 8; i <= linhas; i++) // Pula a linha de cabeçalho
                    for (int i = 2; i <= linhas; i++) // Começa na linha 2 - SITE DA CAIXA
                    {
                        List<int> numeros = new List<int>();
                        for (int j = 3; j <= colunas; j++)
                        {
                            if (int.TryParse(planilha.Cells[i, j].Text, out int numero))
                            {
                                numeros.Add(numero);
                            }
                            else
                            {
                                //numeros.Add(planilha.Cells[i, j].Text);
                            }
                        }
                        //numeros.Sort();
                        jogosSorteados.Add(string.Join(", ", numeros));
                    }

                    return jogosSorteados.ToList();//.Contains(jogo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler o arquivo Excel: {ex.Message}");
                return new List<string>();
            }
        }


        #endregion

        static List<int> CarregaJogosSorteados(string caminhoPlanilha)
        {
            if (!File.Exists(caminhoPlanilha))
            {
                return new List<int>();
            }
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                FileInfo arquivoExcel = new FileInfo(caminhoPlanilha);
                if (!arquivoExcel.Exists)
                {
                    Console.WriteLine("Arquivo Excel não encontrado.");
                    return new List<int>();
                }

                using (ExcelPackage pacote = new ExcelPackage(arquivoExcel))
                {
                    ExcelWorksheet planilha = pacote.Workbook.Worksheets.FirstOrDefault();
                    if (planilha == null)
                    {
                        Console.WriteLine("Planilha não encontrada no arquivo.");
                        return new List<int>();
                    }

                    HashSet<int> jogosSorteados = new HashSet<int>();
                    int colunas = planilha.Dimension.End.Column;
                    int linhas = planilha.Dimension.End.Row;

                    // Lê todos os números da planilha
                    for (int i = 8; i <= linhas; i++) // Pula a linha de cabeçalho
                    {
                        for (int j = 3; j <= colunas; j++)
                        {
                            if (int.TryParse(planilha.Cells[i, j].Text, out int numero))
                            {
                                jogosSorteados.Add(numero);
                            }
                        }
                    }

                    return jogosSorteados.ToList(); // Retorna os números como uma lista de inteiros
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler o arquivo Excel: {ex.Message}");
                return new List<int>();
            }
        }


        /// <summary>
        /// https://loterias.caixa.gov.br/Paginas/Lotofacil.aspx
        /// https://asloterias.com.br/download-todos-resultados-mega-sena
        /// https://asloterias.com.br/download-todos-resultados-lotofacil
        /// </summary>
        /// <param name="typeGame">1-Mega Sena, 2-LotoFacil</param>
        /// <param name="porOrdemCrescente"></param>
        /// <returns></returns>
        public static List<LotoHistoricInfo> GetHistoricInfo(int typeGame, bool porOrdemCrescente = false)
        {
            string ordemInfo = null;
            if (porOrdemCrescente)
            {
                ordemInfo = "crescente";
            }
            else
            {
                ordemInfo = "sorteio";
            }

            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string arquivoExcelHistorico = Path.Combine(exePath, "LOTERICA"
                , (typeGame == 1) ?
                $"mega_sena_{ordemInfo}.xlsx"
                : ((typeGame == 2) ?
                    $"loto_facil_{ordemInfo}.xlsx"
                    : ""
                    )
                );
            List<LotoHistoricInfo> HistoricoJogos = GetJogosSorteadosHistorico(arquivoExcelHistorico);
            return HistoricoJogos;
        }

        static List<LotoHistoricInfo> GetJogosSorteadosHistorico(string caminhoPlanilha)
        {
            if (!File.Exists(caminhoPlanilha))
            {
                return new List<LotoHistoricInfo>();
            }
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                FileInfo arquivoExcel = new FileInfo(caminhoPlanilha);
                if (!arquivoExcel.Exists)
                {
                    Console.WriteLine("Arquivo Excel não encontrado.");
                    return new List<LotoHistoricInfo>();
                }

                using (ExcelPackage pacote = new ExcelPackage(arquivoExcel))
                {
                    ExcelWorksheet planilha = pacote.Workbook.Worksheets.FirstOrDefault();
                    if (planilha == null)
                    {
                        Console.WriteLine("Planilha não encontrada no arquivo.");
                        return new List<LotoHistoricInfo>();
                    }

                    List<LotoHistoricInfo> historico = new List<LotoHistoricInfo>();
                    int colunas = 18;//planilha.Dimension.End.Column;
                    if (caminhoPlanilha.Contains("mega_sena"))
                    {
                        colunas = 8;
                    }
                    int linhas = planilha.Dimension.End.Row;

                    // Lê os dados da planilha
                    int linhaInicio = 2;
                    if (caminhoPlanilha.EndsWith("_sorteio.xlsx"))
                    {
                        linhaInicio = 8;
                    }
                    //for (int i = 8; i <= linhas; i++) // Começa na linha 8
                    for (int i = linhaInicio; i <= linhas; i++) // Começa na linha 2 - SITE DA CAIXA
                    {
                        // Lê o ID da coluna 1
                        if (!int.TryParse(planilha.Cells[i, 1].Text, out int id))
                        {
                            Console.WriteLine($"ID inválido na linha {i}. Ignorando linha.");
                            continue; // Pula para a próxima linha
                        }

                        // Lê a Data da coluna 2
                        if (!DateTime.TryParse(planilha.Cells[i, 2].Text, out DateTime data))
                        {
                            Console.WriteLine($"Data inválida na linha {i}. Ignorando linha.");
                            continue; // Pula para a próxima linha
                        }

                        // Lê os números das colunas 3 em diante
                        HashSet<int> numeros = new HashSet<int>();
                        for (int j = 3; j <= colunas - 1; j++)
                        {
                            if (int.TryParse(planilha.Cells[i, j].Text, out int numero))
                            {
                                numeros.Add(numero);
                            }
                        }

                        // Lê o Ganhadores 15 acertos da coluna 18                        
                        if (int.TryParse(planilha.Cells[i, 18].Text, out int NumeroGanhadores15))
                        {
                        }
                        // Cria a entrada de histórico e adiciona à lista
                        historico.Add(new LotoHistoricInfo
                        {
                            ID = id,
                            Data = data,
                            isGanhadores15Acertos = (NumeroGanhadores15 > 0),
                            Numeros = numeros
                        });
                    }

                    return historico;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler o arquivo Excel: {ex.Message}");
                return new List<LotoHistoricInfo>();
            }
        }


        public static HashSet<HashSet<int>> TranformaHistoricoEmConjuntoParaValidacaoRapida(List<List<int>> historico)
        {
            // Transformar o histórico em um conjunto para validação rápida (considerando números como conjuntos)
            var historicoComoConjuntos = historico
                .Select(h => new HashSet<int>(h))
                .ToHashSet(HashSet<int>.CreateSetComparer());
            return historicoComoConjuntos;
        }
        public static bool VerificaSeJaFoiSorteado(HashSet<int> novaCombinacao, HashSet<HashSet<int>> historicoComoConjuntos)
        {
            // Validar se a nova combinação já existe no histórico ou nas combinações finais
            var novaCombinacaoComoConjunto = new HashSet<int>(novaCombinacao);
            bool isJaFoiSorteada = historicoComoConjuntos.Contains(novaCombinacaoComoConjunto);
            return isJaFoiSorteada;
        }

        public static List<LotoHistoricInfo> VerificarNumeros(HashSet<int> numeros, List<LotoHistoricInfo> historico)
        {
            if (numeros == null || historico == null)
            {
                throw new ArgumentNullException("Os parâmetros não podem ser nulos.");
            }

            // Retorna itens do histórico onde a combinação de números é idêntica
            return historico
                .Where(h => h.Numeros.SetEquals(numeros)) // Verifica se os conjuntos são iguais
                .ToList();

            //return historico
            //    .Where(h => h.Numeros.Any(n => numeros.Contains(n))) // Verifica se há interseção entre os números
            //    .ToList(); // Retorna como lista
        }

        public static bool CombinacaoNoHistorico(HashSet<int> combinacao, List<LotoHistoricInfo> historico)
        {
            // Verifica se algum sorteio do histórico contém todos os números da combinação
            return historico.Any(historicoItem => combinacao.All(numero => historicoItem.Numeros.Contains(numero)));
        }


        /// <summary>
        /// Ordenar a lista principal com base no primeiro elemento de cada sublista
        /// </summary>
        /// <param name="BilhetesParaOrdenar"></param>
        /// <returns></returns>
        public static List<List<int>> OrdenarBilhetes(List<List<int>> BilhetesParaOrdenar)
        {

            return BilhetesParaOrdenar = BilhetesParaOrdenar
               .OrderBy(subLista => subLista.First())
               .ToList();

        }

        /// <summary>
        /// VerificarAcertos/CalcularAcertos: calcula a quantidade de acertos no historico dos jogos
        /// </summary>
        /// <param name="historico"></param>
        /// <param name="novasCombinacoes"></param>
        /// <returns></returns>
        public static Dictionary<List<int>, Dictionary<int, int>> CalcularAcertos(List<List<int>> historico, List<List<int>> novasCombinacoes)
        {
            var resultado = new Dictionary<List<int>, Dictionary<int, int>>();

            foreach (var novaComb in novasCombinacoes)
            {
                // Inicializar o dicionário de acertos por quantidade
                var acertosPorQuantidade = new Dictionary<int, int>();

                foreach (var historicoComb in historico)
                {
                    // Contar os números em comum
                    int acertos = novaComb.Intersect(historicoComb).Count();

                    // Registrar apenas os acertos máximos
                    if (!acertosPorQuantidade.ContainsKey(acertos))
                    {
                        acertosPorQuantidade[acertos] = 0;
                    }
                    acertosPorQuantidade[acertos]++;
                }

                // Adicionar os resultados acumulados
                resultado[novaComb] = acertosPorQuantidade;
            }

            return resultado;
        }

        public static void ExibirResultadosDetalhados(Dictionary<List<int>, Dictionary<int, int>> resultados)
        {
            //Console.Clear();
            Console.WriteLine("\n=== RESULTADOS DETALHADOS ===");
            int idexRow = 0;
            foreach (var resultado in resultados)
            {
                idexRow++;
                var combinacao = string.Join(", ", resultado.Key.OrderBy(x => x));
                Console.WriteLine($"{idexRow}) Combinação: {combinacao}");

                foreach (var acerto in resultado.Value.OrderByDescending(x => x.Key)) // Ordenar por número de acertos
                {
                    Console.WriteLine($"  - Acertos {acerto.Key}: {acerto.Value}");
                }

                if (idexRow % 200 == 0)
                {
                    // Ação para múltiplos de 100
                    Console.WriteLine("-------XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX-------");
                    Console.WriteLine("XXXXXXXXXX-----+100 PRECIONE ALGUMA TECLA PARA CONTINUAR---------------------XXXXXXXXXXX");

                    Console.ReadLine();

                }
            }
        }


    }

    public class LotoHistoricInfo
    {
        public int ID { get; set; }
        public DateTime Data { get; set; }
        public HashSet<int> Numeros { get; set; }
        public bool isGanhadores15Acertos { get; set; }
        public LotoHistoricInfo()
        {
            Numeros = new HashSet<int>();
        }
    }



    public class ResultadoCombinacao
    {
        public List<int> Combinacao { get; set; }
        public int QtdNumerosEmComum { get; set; }
        public int QtdSorteiosAnteriores { get; set; }
        public List<LotoHistoricInfo> lotoHistoricInfo { get; set; }
    }

    public class ResultadoCombinacaoAnalise
    {
        public List<int> Combinacao { get; set; }
        public int TotalTentativas { get; set; }
        public List<LotoHistoricInfo> lotoHistoricInfo { get; set; }
    }

    // Classe para armazenar o resultado da comparação entre uma combinação gerada e um registro histórico
    public class ComparacaoCombinacaoResultado
    {
        public List<int> CombinacaoGerada { get; set; }
        public int HistoricoId { get; set; }
        public int Acertos { get; set; }
        public List<int> NaoAcertados { get; set; }
    }
}
