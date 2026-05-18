using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CSR.Controllers
{
    [Authorize]
    public class ContractController : Controller
    {
        private readonly ContractService _contractService;
        private readonly VendorService _vendorService;
        private readonly ContractFileService _contractFileService;

        public ContractController(ContractService contractService, VendorService vendorService, ContractFileService contractFileService)
        {
            _contractService = contractService;
            _vendorService = vendorService;
            _contractFileService = contractFileService;
        }

        public async Task<IActionResult> Index(string vendorName, int? contractYear, string contractType)
        {
            var contracts = await _contractService.GetContractsAsync(vendorName, contractYear, contractType);

            ViewData["VendorName"] = vendorName;
            ViewData["ContractYear"] = contractYear;
            ViewData["ContractType"] = contractType;

            return View(contracts);
        }

        public async Task<IActionResult> Index2(string vendorName, int? contractYear, string contractType)
        {
            var contracts = await _contractService.GetContractsAsync(vendorName, contractYear, contractType);

            ViewData["VendorName"] = vendorName;
            ViewData["ContractYear"] = contractYear;
            ViewData["ContractType"] = contractType;

            return View(contracts);
        }

        public async Task<IActionResult> Details(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        public async Task<IActionResult> Details2(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(new Contract { CONTRACT_YEAR = DateTime.Now.Year });
        }

        [HttpGet]
        public async Task<IActionResult> Create2()
        {
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(new Contract { CONTRACT_YEAR = DateTime.Now.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, List<IFormFile> files)
        {
            ModelState.Remove("REG_DATE");
            ModelState.Remove("REG_USERID");

            if (ModelState.IsValid)
            {
                contract.REG_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var newContractId = await _contractService.CreateContractAsync(contract);

                if (files != null && files.Count > 0)
                    await _contractFileService.AddContractFilesAsync(newContractId, files, contract.REG_USERID);

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(contract);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _contractService.GetContractByIdAsync(id);
            if (contract == null) return NotFound();
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Contract contract, List<IFormFile> newFiles, [FromForm] List<int> deletedFiles)
        {
            ModelState.Remove("UPDATE_DATE");
            ModelState.Remove("UPDATE_USERID");

            if (ModelState.IsValid)
            {
                contract.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _contractService.UpdateContractAsync(contract);

                if (deletedFiles != null && deletedFiles.Count > 0)
                {
                    foreach (var fileId in deletedFiles)
                        await _contractFileService.DeleteContractFileAsync(fileId);
                }

                if (newFiles != null && newFiles.Count > 0)
                    await _contractFileService.AddContractFilesAsync(contract.CONTRACT_ID, newFiles, contract.UPDATE_USERID);

                return RedirectToAction(nameof(Details), new { id = contract.CONTRACT_ID });
            }

            contract.AttachFiles = (await _contractFileService.GetContractFilesAsync(contract.CONTRACT_ID)).ToList();
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(contract);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _contractService.DeleteContractAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _contractFileService.GetContractFileByIdAsync(id);
            if (file == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FILEPATH);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            return PhysicalFile(filePath, "application/octet-stream", file.REAL_FILENAME);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId, int contractId)
        {
            await _contractFileService.DeleteContractFileAsync(fileId);
            return RedirectToAction(nameof(Details), new { id = contractId });
        }
    }
}
