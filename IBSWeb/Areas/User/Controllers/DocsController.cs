using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace IBSWeb.Areas.User.Controllers;

[Area("User")]
[Route("User/Docs")]
[Authorize]
public class DocsController : Controller
{
    private static readonly string[] ManualFiles =
    [
        "01-job-order",
        "02-dispatch-ticket",
        "03-billing",
        "04-collection",
        "05-service-request",
        "06-master-files",
        "07-admin",
        "08-import-export",
        "09-reports",
        "10-notifications-audit"
    ];

    private readonly string _docsRoot;
    private readonly MarkdownPipeline _pipeline;

    public DocsController(IWebHostEnvironment env)
    {
        _docsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "Docs", "manual"));
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var html = RenderMarkdownFile("README");
        var sidebar = GetSidebarItems();
        ViewBag.Sidebar = sidebar;
        ViewBag.ActiveSlug = "index";
        ViewBag.Title = "User Manual";
        return View("Index", html);
    }

    [HttpGet("{slug}")]
    public IActionResult Show(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction(nameof(Index));

        if (slug == "index" || slug == "README")
            return RedirectToAction("Index");

        var fileSlug = ManualFiles.FirstOrDefault(f =>
            f.EndsWith(slug, StringComparison.OrdinalIgnoreCase) ||
            f.Replace("0", "").Replace("-", "") == slug.Replace("-", ""));

        if (fileSlug == null)
            return NotFound();

        var html = RenderMarkdownFile(fileSlug);
        if (html == null)
            return NotFound();

        var sidebar = GetSidebarItems();
        ViewBag.Sidebar = sidebar;
        ViewBag.ActiveSlug = slug;
        ViewBag.Title = GetTitleFromFile(fileSlug);
        return View("Index", html);
    }

    private string? RenderMarkdownFile(string slug)
    {
        var filePath = slug == "README"
            ? Path.Combine(_docsRoot, "README.md")
            : Path.Combine(_docsRoot, $"{slug}.md");

        if (!System.IO.File.Exists(filePath))
            return null;

        var markdown = System.IO.File.ReadAllText(filePath);
        return Markdown.ToHtml(markdown, _pipeline);
    }

    private static string GetTitleFromFile(string slug)
    {
        var match = Regex.Match(slug, @"^\d+-(.+)$");
        if (!match.Success)
            return "User Manual";

        var name = match.Groups[1].Value.Replace("-", " ");
        return char.ToUpper(name[0]) + name[1..];
    }

    private static List<(string Slug, string Title)> GetSidebarItems()
    {
        var items = new List<(string Slug, string Title)>
        {
            ("index", "Overview"),
            ("job-order", "Job Order"),
            ("dispatch-ticket", "Dispatch Ticket"),
            ("billing", "Billing"),
            ("collection", "Collection"),
            ("service-request", "Service Request"),
            ("master-files", "Master Files"),
            ("admin", "Administration"),
            ("import-export", "Import & Export"),
            ("reports", "Reports"),
            ("notifications-audit", "Notifications & Audit")
        };
        return items;
    }
}
