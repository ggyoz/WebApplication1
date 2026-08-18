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
    public class VendorController : Controller
    {
        private readonly VendorService _vendorService;

        public VendorController(VendorService vendorService)
        {
            _vendorService = vendorService;
        }

        public async Task<IActionResult> Index(string vendorName, string vendorCode, string vendorType, string status)
        {
            var vendors = await _vendorService.GetVendorsAsync(vendorName, vendorCode, vendorType, status);
            
            ViewData["VendorName"] = vendorName;
            ViewData["VendorCode"] = vendorCode;
            ViewData["VendorType"] = vendorType;
            ViewData["Status"] = status;

            return View(vendors);
        }
        public async Task<IActionResult> Index2(string vendorName, string vendorCode, string vendorType, string status)
        {
            var vendors = await _vendorService.GetVendorsAsync(vendorName, vendorCode, vendorType, status);
            
            ViewData["VendorName"] = vendorName;
            ViewData["VendorCode"] = vendorCode;
            ViewData["VendorType"] = vendorType;
            ViewData["Status"] = status;

            return View(vendors);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();
            return View(vendor);
        }

        
        public async Task<IActionResult> Details2(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();
            return View(vendor);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new Vendor());
        }

        [HttpGet]
        public IActionResult Create2()
        {
            return View(new Vendor());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                vendor.REG_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _vendorService.CreateVendorAsync(vendor);
                return RedirectToAction(nameof(Index));
            }
            return View(vendor);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();
            return View(vendor);
        }

        [HttpGet]
        public async Task<IActionResult> Edit2(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                vendor.UPDATE_USERID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _vendorService.UpdateVendorAsync(vendor);
                return RedirectToAction(nameof(Index));
            }
            return View(vendor);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _vendorService.DeleteVendorAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
