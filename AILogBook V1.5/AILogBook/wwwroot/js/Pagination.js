    const rowsPerPage = 5; // change as needed
    const table = document.querySelector("table tbody");
    const rows = table.querySelectorAll("tr");
    const totalRows = rows.length;

    const pagination = document.getElementById("pagination");
    const tableInfo = document.getElementById("tableInfo");

    let currentPage = 1;
    const totalPages = Math.ceil(totalRows / rowsPerPage);

    function showPage(page) {
        currentPage = page;

    let start = (page - 1) * rowsPerPage;
    let end = start + rowsPerPage;

        rows.forEach((row, index) => {
        row.style.display = (index >= start && index < end) ? "" : "none";
        });

    updatePagination();
    updateInfo();
    }

    function updatePagination() {
        pagination.innerHTML = "";

    // Previous
    pagination.innerHTML += `
    <li class="page-item ${currentPage === 1 ? " disabled" : ""}">
    <a class="page-link" href="#" onclick="showPage(${currentPage - 1})">Previous</a>
</li>
`;

        // Pages
        for (let i = 1; i <= totalPages; i++) {
            pagination.innerHTML += `
    < li class="page-item ${i === currentPage ? "active" : ""}" >
        <a class="page-link" href="#" onclick="showPage(${i})">${i}</a>
                </li >
    `;
        }

        // Next
        pagination.innerHTML += `
    < li class="page-item ${currentPage === totalPages ? "disabled" : ""}" >
        <a class="page-link" href="#" onclick="showPage(${currentPage + 1})">Next</a>
            </li >
    `;
    }

    function updateInfo() {
        let start = (currentPage - 1) * rowsPerPage + 1;
        let end = Math.min(currentPage * rowsPerPage, totalRows);

        tableInfo.innerText = `Showing ${ start } to ${ end } of ${ totalRows } entries`;
    }

    // Init
    showPage(1);
