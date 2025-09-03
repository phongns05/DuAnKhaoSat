using OfficeOpenXml;
using SurveySystem.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace SurveySystem.Services
{
    public class ExcelService
    {
        private readonly AppDbContext _context;

        public ExcelService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string message, int importedCount)> ImportQuestionsFromExcelAsync(IFormFile file, int createdBy)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return (false, "Vui lòng chọn file Excel", 0);

                if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return (false, "Chỉ hỗ trợ file Excel (.xlsx)", 0);

                // Configure EPPlus license for non-commercial use
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage(file.OpenReadStream());
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return (false, "Không tìm thấy worksheet trong file Excel", 0);

                var importedCount = 0;
                var errors = new List<string>();

                // Bắt đầu từ dòng 2 (dòng 1 là header)
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    try
                    {
                        var question = new Question
                        {
                            Content = GetCellValue(worksheet, row, 1), // Nội dung câu hỏi
                            QuestionType = GetCellValue(worksheet, row, 2), // Loại câu hỏi
                            SkillId = await GetSkillIdByName(GetCellValue(worksheet, row, 3)), // Kỹ năng
                            DifficultyId = await GetDifficultyIdByName(GetCellValue(worksheet, row, 4)), // Độ khó
                            CreatedBy = createdBy,
                            CreatedDate = DateTime.UtcNow
                        };

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(question.Content))
                        {
                            errors.Add($"Dòng {row}: Nội dung câu hỏi không được để trống");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(question.QuestionType))
                        {
                            errors.Add($"Dòng {row}: Loại câu hỏi không được để trống");
                            continue;
                        }

                        // Validate question type
                        if (!new[] { "MCQ", "TrueFalse", "Essay" }.Contains(question.QuestionType))
                        {
                            errors.Add($"Dòng {row}: Loại câu hỏi không hợp lệ (MCQ/TrueFalse/Essay)");
                            continue;
                        }

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();

                        // Add options for MCQ and TrueFalse
                        if (question.QuestionType == "MCQ" || question.QuestionType == "TrueFalse")
                        {
                            var options = GetCellValue(worksheet, row, 5); // Đáp án
                            var correctAnswers = GetCellValue(worksheet, row, 6); // Đáp án đúng

                            if (!string.IsNullOrWhiteSpace(options))
                            {
                                var optionList = options.Split('|').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToList();
                                var correctList = correctAnswers?.Split('|').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToList() ?? new List<string>();

                                foreach (var option in optionList)
                                {
                                    var questionOption = new QuestionOption
                                    {
                                        QuestionId = question.QuestionId,
                                        Content = option,
                                        IsCorrect = correctList.Contains(option)
                                    };
                                    _context.QuestionOptions.Add(questionOption);
                                }
                                await _context.SaveChangesAsync();
                            }
                        }

                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Dòng {row}: {ex.Message}");
                    }
                }

                var message = importedCount > 0 
                    ? $"Import thành công {importedCount} câu hỏi" + (errors.Any() ? $". Lỗi: {string.Join(", ", errors)}" : "")
                    : $"Import thất bại. Lỗi: {string.Join(", ", errors)}";

                return (importedCount > 0, message, importedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xử lý file: {ex.Message}", 0);
            }
        }

        public byte[] ExportQuestionsToExcel()
        {
            // Configure EPPlus license for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");

            // Headers
            worksheet.Cells[1, 1].Value = "Nội dung câu hỏi";
            worksheet.Cells[1, 2].Value = "Loại câu hỏi (MCQ/TrueFalse/Essay)";
            worksheet.Cells[1, 3].Value = "Kỹ năng";
            worksheet.Cells[1, 4].Value = "Độ khó";
            worksheet.Cells[1, 5].Value = "Đáp án (phân cách bằng |)";
            worksheet.Cells[1, 6].Value = "Đáp án đúng (phân cách bằng |)";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            return worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
        }

        private async Task<int?> GetSkillIdByName(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
                return null;

            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillName.ToLower() == skillName.ToLower());
            return skill?.SkillId;
        }

        private async Task<int?> GetDifficultyIdByName(string difficultyName)
        {
            if (string.IsNullOrWhiteSpace(difficultyName))
                return null;

            var difficulty = await _context.Difficulties.FirstOrDefaultAsync(d => d.LevelName.ToLower() == difficultyName.ToLower());
            return difficulty?.DifficultyId;
        }
    }
}
