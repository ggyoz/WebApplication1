using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CSR.Models;
using CSR.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CSR.Controllers
{
    [Authorize]
    public class ContractController : Controller
    {
        private readonly ContractService _contractService;
        private readonly VendorService _vendorService;

        public ContractController(ContractService contractService, VendorService vendorService)
        {
            _contractService = contractService;
            _vendorService = vendorService;
        }

        public async Task<IActionResult> Index(string vendorName, int? contractYear, string contractType)
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

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(new Contract { CONTRACT_YEAR = DateTime.Now.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.REG_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _contractService.CreateContractAsync(contract);
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
        public async Task<IActionResult> Edit(Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _contractService.UpdateContractAsync(contract);
                return RedirectToAction(nameof(Details), new { id = contract.CONTRACT_ID });
            }
            ViewBag.Vendors = await _vendorService.GetVendorsAsync();
            return View(contract);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _contractService.DeleteContractAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
