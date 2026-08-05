const searchBox = document.getElementById("searchBox");
const dropdown = document.getElementById("dropdownList");
const items = dropdown.getElementsByTagName("div");

// Show all on click
searchBox.addEventListener("focus", function () {
	dropdown.style.display = "block";
});

// Filter on typing
searchBox.addEventListener("keyup", function () {
	let filter = this.value.toLowerCase();
	dropdown.style.display = "block";

	for (let i = 0; i < items.length; i++) {
		let txt = items[i].textContent.toLowerCase();
		items[i].style.display = txt.includes(filter) ? "" : "none";
	}
});

// Select item
for (let i = 0; i < items.length; i++) {
	items[i].addEventListener("click", function () {

		// Show text
		searchBox.value = this.textContent;

		// Store ID
		document.getElementById("fruitId").value = this.getAttribute("data-id");

		dropdown.style.display = "none";
	});
}
document.querySelectorAll("#dropdownList div").forEach(item => {
	item.addEventListener("click", function () {

		document.getElementById("searchBox").value = this.innerText;

		document.getElementById("fruitId").value = this.getAttribute("data-id");

		document.getElementById("dropdownList").style.display = "none";
	});
});
document.addEventListener("click", function (e) {
	if (!e.target.closest("form")) {
		dropdown.style.display = "none";
	}
});