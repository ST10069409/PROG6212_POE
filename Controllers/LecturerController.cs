using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PROG6212_Part1.Models;

namespace PROG6212_Part1.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IConfiguration _configuration;

        public LecturerController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult SubmitAndTrackClaim()
        {
            var claims = GetAllClaimsFromDatabase(); // Fetch claims from database
            return View(claims);
        }

        [HttpPost]
        public IActionResult SubmitClaim(string lecturer, string modulecode, int hoursWorked, decimal hourlyRate, string notes, IFormFile document)
        {
            if (hoursWorked <= 0 || hourlyRate <= 0 || document == null)
            {
                ViewBag.ErrorMessage = "Please provide valid data and upload a document.";
                return View("SubmitAndTrackClaim", GetAllClaimsFromDatabase());
            }

            string[] allowedExtensions = { ".pdf", ".docx", ".xlsx" };
            var fileExtension = Path.GetExtension(document.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewBag.ErrorMessage = "Invalid file type. Only .pdf, .docx, and .xlsx files are allowed.";
                return View("SubmitAndTrackClaim", GetAllClaimsFromDatabase());
            }

            if (document.Length > 5 * 1024 * 1024)
            {
                ViewBag.ErrorMessage = "File size exceeds 5MB.";
                return View("SubmitAndTrackClaim", GetAllClaimsFromDatabase());
            }

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            int totalAmount = (int)(hoursWorked * hourlyRate);
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(document.FileName)}";
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                document.CopyTo(fileStream);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO Claims (Lecturer, ModuleCode, HoursWorked, HourlyRate, TotalAmount, Notes, DocumentPath, Status)
                    VALUES (@Lecturer, @ModuleCode, @HoursWorked, @HourlyRate, @TotalAmount, @Notes, @DocumentPath, 'Pending')";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Lecturer", lecturer);
                    cmd.Parameters.AddWithValue("@ModuleCode", modulecode);
                    cmd.Parameters.AddWithValue("@HoursWorked", hoursWorked);
                    cmd.Parameters.AddWithValue("@HourlyRate", hourlyRate);
                    cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("@DocumentPath", $"/uploads/{uniqueFileName}");

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Claim submitted successfully.";
            return RedirectToAction("SubmitAndTrackClaim");
        }

        [HttpPost]
        public IActionResult AutomateClaimsProcessing()
        {
            var claims = GetAllClaimsFromDatabase();

            foreach (var claim in claims)
            {
                if (claim.HoursWorked < 5)
                {
                    claim.Status = "Rejected";
                }
                else
                {
                    claim.Status = "Approved";
                }

                UpdateClaimStatusInDatabase(claim.ClaimId, claim.Status);
            }

            TempData["SuccessMessage"] = "Claims processed successfully.";
            return RedirectToAction("SubmitAndTrackClaim");
        }

        private List<Claim> GetAllClaimsFromDatabase()
        {
            var claims = new List<Claim>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Claims";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            claims.Add(new Claim
                            {
                                ClaimId = reader.GetInt32(0),
                                Lecturer = reader.GetString(1),
                                ModuleCode = reader.GetString(2),
                                HoursWorked = reader.GetInt32(3),
                                HourlyRate = (int)reader.GetDecimal(4), // Explicit cast
                                TotalAmount = (int)reader.GetDecimal(5), // Explicit cast
                                Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                                DocumentPath = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Status = reader.GetString(8)
                            });
                        }
                    }
                }
            }

            return claims;
        }

        private void UpdateClaimStatusInDatabase(int claimId, string status)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Claims SET Status = @Status WHERE ClaimId = @ClaimId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@ClaimId", claimId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

