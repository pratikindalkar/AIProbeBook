//Search and Show entries drop box
let currentPage = 1;
function filterTable() {
    let searchValue = document.getElementById("searchInput").value.toLowerCase();
    let limit = parseInt(document.getElementById("entriesSelect").value);

    let rows = document.querySelectorAll("tbody tr");
    let nextBtn = document.getElementById("nextPageBtn");
    let prevBtn = document.getElementById("prevPageBtn");

    let matchedRows = [];

    rows.forEach(row => {
        let text = row.innerText.toLowerCase();

        if (text.includes(searchValue)) {
            matchedRows.push(row);
        }

        row.style.display = "none";
    });

    let totalPages = Math.ceil(matchedRows.length / limit);

    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    let startIndex = (currentPage - 1) * limit;
    let endIndex = startIndex + limit;

    matchedRows.forEach((row, index) => {
        if (index >= startIndex && index < endIndex) {
            row.style.display = "";
        }
    });

    prevBtn.disabled = (currentPage === 1);
    nextBtn.disabled = (currentPage === totalPages || totalPages === 0);
}
document.getElementById("searchInput").addEventListener("keyup", () => {
    currentPage = 1;
    filterTable();
});

document.getElementById("entriesSelect").addEventListener("change", () => {
    currentPage = 1;
    let limit = document.getElementById("entriesSelect").value;
    sessionStorage.setItem("entriesLimit", limit);
    filterTable();
});

document.getElementById("nextPageBtn").addEventListener("click", () => {
    currentPage++;
    sessionStorage.setItem("currentPage", currentPage);
    filterTable();
});

document.getElementById("prevPageBtn").addEventListener("click", () => {
    currentPage--;
    sessionStorage.setItem("currentPage", currentPage);
    filterTable();
});
document.addEventListener("DOMContentLoaded", () => {
    let savedPage = sessionStorage.getItem("currentPage");
    let savedLimit = sessionStorage.getItem("entriesLimit");
    if (savedPage) {
        currentPage = parseInt(savedPage);
    } else {
        currentPage = 1;
    }
    if (savedLimit) {
        document.getElementById("entriesSelect").value = savedLimit;
    }

    filterTable();
});