using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using PROG6212_Part1.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace PROG6212_Part1.Controllers
{
    public class HumanResourceController : Controller
    {
        private readonly IConfiguration _configuration;

        public HumanResourceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: Display all claims
        public IActionResult HRview()
        {
            var claims = GetAllClaims();
            return View(claims); // Pass claims to the view
        }

        // GET: Load claim details for editing
        public IActionResult EditClaim(int claimId)
        {
            var claim = GetClaimById(claimId);
            if (claim == null)
            {
                return NotFound(); // If the claim is not found
            }

            return View("EditClaim", claim); // Pass claim to the edit view
        }

        // POST: Update claim information
        [HttpPost]
        public IActionResult UpdateClaim(int claimId, string moduleCode, string notes)
        {
            try
            {
                if (UpdateClaimInDatabase(claimId, moduleCode, notes))
                {
                    TempData["SuccessMessage"] = "Claim updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update the claim.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            // Redirect to the HRview action
            return RedirectToAction("HRview");
        }

        // Method to get all claims from the database
        private List<Claim> GetAllClaims()
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
                                HoursWorked = reader.GetInt32(3), // Assume HoursWorked is an int
                                HourlyRate = reader.IsDBNull(4) ? 0 : (int)Math.Round(reader.GetDecimal(4)), // Explicit conversion
                                TotalAmount = reader.IsDBNull(5) ? 0 : (int)Math.Round(reader.GetDecimal(5)), // Explicit conversion
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

        // Method to get a single claim by ID
        private Claim GetClaimById(int claimId)
        {
            Claim claim = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Claims WHERE ClaimId = @ClaimId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ClaimId", claimId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            claim = new Claim
                            {
                                ClaimId = reader.GetInt32(0),
                                Lecturer = reader.GetString(1),
                                ModuleCode = reader.GetString(2),
                                HoursWorked = reader.GetInt32(3),
                                HourlyRate = reader.IsDBNull(4) ? 0 : (int)Math.Round(reader.GetDecimal(4)), // Explicit conversion
                                TotalAmount = reader.IsDBNull(5) ? 0 : (int)Math.Round(reader.GetDecimal(5)), // Explicit conversion
                                Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                                DocumentPath = reader.GetString(7),
                                Status = reader.GetString(8)
                            };
                        }
                    }
                }
            }

            return claim;
        }

        // Method to update claim information in the database
        private bool UpdateClaimInDatabase(int claimId, string moduleCode, string notes)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Claims SET ModuleCode = @ModuleCode, Notes = @Notes WHERE ClaimId = @ClaimId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ModuleCode", moduleCode);
                    cmd.Parameters.AddWithValue("@Notes", notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ClaimId", claimId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}


