using System;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace ParentProcess.Test
{
    [TestFixture]
    public class ParentedProcessManagerFixture
    {
        [TestCase]
        public void ParentProcessManagerConstructor_WithNullFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ParentedProcessManager(null, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithEmptyFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ParentedProcessManager(string.Empty, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithNotFoundFilename_ThrowsFileNotFoundException()
        {
            // Arrange + Act + Assert
            Assert.Throws<FileNotFoundException>(() => { new ParentedProcessManager(@"C:\Foo\Bar.exe", null, null); });
        }

        private Exception _unhandledException;

        [TestCase]
        public void ParentProcessManager_WithFaultingApplication_FiresUnhandledExceptionEvent()
        {
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var finalPath = Path.Combine(path, @"..\..\..\ParentProcess.Test.FaultingAppTest\bin\Debug\ParentProcess.Test.FaultingAppTest.exe");
            // Arrange
            var manager = new ParentedProcessManager(finalPath, "Faulting process", "Faulting process");
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(6000);

            // Assert

            Assert.That(_unhandledException, Is.Not.Null);
            Assert.That(manager.IsRunning, Is.False);
        }

        private bool _wasStopped = false;

        [TestCase]
        public void ParentProcessManager_WhenKillingApplication_FiresStoppedEvent()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ParentedProcessManager(path, "Notepad will be killed", "Notepad will be killed");
            manager.ProcessStoppedEvent += Manager_ProcessStoppedEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(1000);

            manager.StopProcess();

            Thread.Sleep(1000);

            // Assert

            Assert.That(_wasStopped, Is.True);
            Assert.That(manager.IsRunning, Is.False);
        }

        private void Manager_ProcessStoppedEvent(EventArgs args)
        {
            _wasStopped = true;
        }

        private void Manager_ProcessUnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs args)
        {
            _unhandledException = args.ExceptionObject as Exception;
        }

        [TestCase]
        public void ParentProcessManager_StartProcessWithRunningApplication_HasIsRunningTrue()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ParentedProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            manager.StartProcess();

            Thread.Sleep(2000);

            // Assert
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.IsRunning, Is.True);

            // Clean up
            manager.StopProcess();
        }

        private string _mainWindowHandle = string.Empty;
        private ParentedProcessManager _manager;

        [TestCase]
        public void ParentProcessManager_StartProcessWithMainWindow_CanFindMainWindowHandle()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            _manager = new ParentedProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            _manager.StartProcess();
            _manager.ProcessMainWindowHandleFoundEvent += Manager_ProcessMainWindowHandleFoundEvent;
            Thread.Sleep(2000);
            _manager.FindMainWindowHandle();
            Thread.Sleep(2000);

            // Assert
            Assert.That(_manager, Is.Not.Null);
            Assert.That(_manager.IsRunning, Is.True);
            Assert.That(_mainWindowHandle, Is.Not.Null);
            Assert.That(_mainWindowHandle, Is.Not.Empty);
            Assert.That(_mainWindowHandle, Is.Not.EqualTo("-1"));

            // Clean up
            _manager.StopProcess();
        }

        private void Manager_ProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            _mainWindowHandle = _manager.ParentedProcessInfo.MainWindowHandle.ToString();
        }
    }
}