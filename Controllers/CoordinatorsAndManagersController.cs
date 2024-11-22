using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using PROG6212_Part1.Models;
using Microsoft.Extensions.Configuration;

namespace PROG6212_Part1.Controllers
{
    public class CoordinatorsAndManagersController : Controller
    {
        // Commented out the static list to avoid ENC0033 error during runtime
        // private static List<Claim> claimsList = new List<Claim>();

        private readonly IConfiguration _configuration;

        public CoordinatorsAndManagersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult ReviewClaims()
        {
            var claimsList = GetAllClaimsFromDatabase(); // Fetch claims from database
            return View(claimsList);
        }

        [HttpPost]
        public IActionResult ApproveClaim(int claimId)
        {
            try
            {
                UpdateClaimStatusInDatabase(claimId, "Approved");
                TempData["SuccessMessage"] = "Claim approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to approve claim: {ex.Message}";
            }

            return RedirectToAction("ReviewClaims");
        }

        [HttpPost]
        public IActionResult RejectClaim(int claimId)
        {
            try
            {
                UpdateClaimStatusInDatabase(claimId, "Rejected");
                TempData["SuccessMessage"] = "Claim rejected successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to reject claim: {ex.Message}";
            }

            return RedirectToAction("ReviewClaims");
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
                                HourlyRate = reader.IsDBNull(4) ? 0 : (int)Math.Round(reader.GetDecimal(4)),
                                TotalAmount = reader.IsDBNull(5) ? 0 : (int)Math.Round(reader.GetDecimal(5)),
                                Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                                DocumentPath = reader.GetString(7),
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

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected. Claim ID may not exist.");
                    }
                }
            }
        }
    }
}
